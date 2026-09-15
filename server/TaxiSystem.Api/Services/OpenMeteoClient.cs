using System.Text.Json;

namespace TaxiSystem.Api.Services;

public record WeatherForecast(string HazardLevel, double PrecipitationMm, string Source);

/// <summary>
/// Клієнт до безкоштовного REST API Open-Meteo (без ключа). Перевіряє опади
/// "зараз" (поточний блок `current`) і на найближчу годину (`minutely_15`) —
/// саме в момент, коли пасажир формує замовлення: чи вже мокрий асфальт, чи
/// от-от почнеться дощ. Це навмисно НЕ прогноз на добу наперед — далекий
/// прогноз тут не має сенсу, бо рішення (запропонувати безпечний маршрут)
/// приймається одноразово, під конкретне замовлення, а не завчасно.
/// </summary>
public class OpenMeteoClient
{
    private const double HazardThresholdMm = 0.1;

    private readonly HttpClient _http;

    public OpenMeteoClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<WeatherForecast> GetHazardForecastAsync(double lat, double lng, CancellationToken ct = default)
    {
        var url = $"https://api.open-meteo.com/v1/forecast?latitude={lat.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
                  $"&longitude={lng.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
                  "&current=precipitation&minutely_15=precipitation&forecast_days=1&timezone=auto";

        try
        {
            using var response = await _http.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            // Опади прямо зараз (чи вже мокрий асфальт).
            var currentPrecipitation = doc.RootElement
                .GetProperty("current")
                .GetProperty("precipitation")
                .GetDouble();

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

            var worstCase = Math.Max(currentPrecipitation, nextHourMax);
            var hazard = worstCase >= HazardThresholdMm ? "HIGH" : "NORMAL";

            return new WeatherForecast(hazard, worstCase, "open-meteo.com");
        }
        catch (Exception)
        {
            // Open-Meteo недоступний/мережева помилка — не валимо запит клієнта,
            // повертаємо безпечний дефолт з позначеним джерелом.
            return new WeatherForecast("NORMAL", 0, "unavailable");
        }
    }
}
