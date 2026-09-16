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

    /// <summary>WMO weather_code для будь-яких активних опадів (мряка/дощ/злива/сніг/гроза) —
    /// модель Open-Meteo сама класифікує погоду цим кодом незалежно від округленої
    /// кількості мм. Трапляється, що дуже легкий дощ округлюється до 0.00мм у
    /// precipitation, але weather_code все одно "61 — Rain: Slight" — тобто лише
    /// порогу по мм замало, потрібно довіряти й цій класифікації.</summary>
    private static readonly HashSet<int> ActivePrecipitationCodes = new()
    {
        51, 53, 55, // мряка (легка/помірна/сильна)
        56, 57,     // замерзаюча мряка
        61, 63, 65, // дощ (легкий/помірний/сильний)
        66, 67,     // замерзаючий дощ
        71, 73, 75, 77, // сніг
        80, 81, 82, // зливи
        85, 86,     // снігові зливи
        95, 96, 99, // гроза
    };

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
            var currentTime = current.GetProperty("time").GetString();

            // "minutely_15" насправді починається з ПІВНОЧІ поточної доби
            // (як і "hourly"), а не з поточного моменту — тож щоб дістати
            // "найближчу годину вперед", треба знайти індекс, що відповідає
            // current.time, і брати наступні 4 значення (4 × 15 хв) від нього,
            // а не сліпо перші 4 елементи масиву (це й був баг: перевірялась
            // північ, а не "зараз").
            var minutely = doc.RootElement.GetProperty("minutely_15");
            var minutelyTimes = minutely.GetProperty("time").EnumerateArray().Select(t => t.GetString()).ToList();
            var minutelyPrecip = minutely.GetProperty("precipitation").EnumerateArray().Select(v => v.GetDouble()).ToList();

            var startIndex = minutelyTimes.IndexOf(currentTime);
            if (startIndex < 0) startIndex = 0;

            var nextHourMax = minutelyPrecip
                .Skip(startIndex)
                .Take(4)
                .DefaultIfEmpty(0)
                .Max();

            var worstCasePrecip = Math.Max(currentPrecipitation, nextHourMax);

            // Кілька незалежних причин, чому дорога може бути слизькою — будь-якої
            // з них досить, щоб вважати умови небезпечними (не тільки "йде дощ").
            var reasons = new List<string>();
            if (worstCasePrecip >= HazardThresholdMm) reasons.Add("опади (дощ/мряка)");
            if (FreezingPrecipitationCodes.Contains(weatherCode)) reasons.Add("замерзаючі опади (ожеледиця)");
            if (temperature <= FrostTemperatureC) reasons.Add("низька температура (ризик інею/льоду)");
            if (worstCasePrecip < HazardThresholdMm && ActivePrecipitationCodes.Contains(weatherCode))
                reasons.Add("опади за класифікацією погоди (кількість округлилась до 0мм)");

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
