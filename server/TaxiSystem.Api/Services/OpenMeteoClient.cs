using System.Text.Json;

namespace TaxiSystem.Api.Services;

public record WeatherForecast(string HazardLevel, double PrecipitationMm, string Source, string Reason);

/// <summary>
/// Клієнт до безкоштовного REST API Open-Meteo (без ключа). Перевіряє не
/// лише дощ, а комплекс умов, що роблять дорогу слизькою "зараз" (поточний
/// блок `current`) і на найближчу годину (`minutely_15`): опади (дощ/мряка),
/// замерзаючі опади (ожеледиця) і ризик інею при температурі близько нуля
/// вранці. Це навмисно НЕ прогноз на добу наперед — далекий прогноз тут не
/// має сенсу, бо рішення (запропонувати безпечний маршрут) приймається
/// одноразово, під конкретне замовлення, а не завчасно.
/// </summary>
public class OpenMeteoClient
{
    private const double HazardThresholdMm = 0.1;

    /// <summary>Температура, нижче якої на дорозі можливий іній/ожеледиця навіть без опадів.</summary>
    private const double FrostTemperatureC = 1.0;

    /// <summary>WMO weather_code для замерзаючих опадів (freezing drizzle/rain) — пряма ожеледиця.</summary>
    private static readonly HashSet<int> FreezingPrecipitationCodes = new() { 56, 57, 66, 67 };

    private readonly HttpClient _http;

    public OpenMeteoClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<WeatherForecast> GetHazardForecastAsync(double lat, double lng, CancellationToken ct = default)
    {
        var url = $"https://api.open-meteo.com/v1/forecast?latitude={lat.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
                  $"&longitude={lng.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
                  "&current=precipitation,temperature_2m,weather_code&minutely_15=precipitation&forecast_days=1&timezone=auto";

        try
        {
            using var response = await _http.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            var current = doc.RootElement.GetProperty("current");

            // Опади прямо зараз (чи вже мокрий асфальт).
            var currentPrecipitation = current.GetProperty("precipitation").GetDouble();
            var temperature = current.GetProperty("temperature_2m").GetDouble();
            var weatherCode = current.GetProperty("weather_code").GetInt32();

            // "minutely_15" завжди починається з поточного моменту (на відміну від
            // "hourly", де масив стартує з 00:00 доби) — тож перші 4 значення
            // (4 × 15 хв) це чесно "найближча година вперед".
            var nextHourMax = doc.RootElement
                .GetProperty("minutely_15")
                .GetProperty("precipitation")
                .EnumerateArray()
                .Take(4)
                .Select(v => v.GetDouble())
                .DefaultIfEmpty(0)
                .Max();

            var worstCasePrecip = Math.Max(currentPrecipitation, nextHourMax);

            // Кілька незалежних причин, чому дорога може бути слизькою — будь-якої
            // з них досить, щоб вважати умови небезпечними (не тільки "йде дощ").
            var reasons = new List<string>();
            if (worstCasePrecip >= HazardThresholdMm) reasons.Add("опади (дощ/мряка)");
            if (FreezingPrecipitationCodes.Contains(weatherCode)) reasons.Add("замерзаючі опади (ожеледиця)");
            if (temperature <= FrostTemperatureC) reasons.Add("низька температура (ризик інею/льоду)");

            var hazard = reasons.Count > 0 ? "HIGH" : "NORMAL";
            var reasonText = reasons.Count > 0 ? string.Join(", ", reasons) : "без опадів, дорога суха";

            return new WeatherForecast(hazard, worstCasePrecip, "open-meteo.com", reasonText);
        }
        catch (Exception)
        {
            // Open-Meteo недоступний/мережева помилка — не валимо запит клієнта,
            // повертаємо безпечний дефолт з позначеним джерелом.
            return new WeatherForecast("NORMAL", 0, "unavailable", "прогноз недоступний");
        }
    }
}
