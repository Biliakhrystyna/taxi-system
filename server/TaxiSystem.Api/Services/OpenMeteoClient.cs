using System.Text.Json;

namespace TaxiSystem.Api.Services;

public record WeatherForecast(string HazardLevel, double PrecipitationMm, string Source, string Reason);

/// <summary>
/// Клієнт до REST API Open-Meteo (без ключа): перевіряє, чи дорога слизька зараз
/// і протягом найближчої години (опади, ожеледиця, іній, сніг).
/// </summary>
public class OpenMeteoClient
{
    private const double HazardThresholdMm = 0.1;

    /// <summary>Температура, нижче якої на дорозі можливий іній/ожеледиця навіть без опадів.</summary>
    private const double FrostTemperatureC = 1.0;

    /// <summary>Скільки 15-хвилинних інтервалів назад дивитись на "щойно був дощ" (8 × 15 хв = 2 години).</summary>
    private const int PastHazardWindowSlots = 8;

    /// <summary>Мінімальна глибина снігу (м), яку вважаємо реальним покривом.</summary>
    private const double SnowDepthThresholdM = 0.01;

    /// <summary>WMO-коди замерзаючих опадів (ожеледиця).</summary>
    private static readonly HashSet<int> FreezingPrecipitationCodes = new() { 56, 57, 66, 67 };

    /// <summary>WMO-коди будь-яких активних опадів: легкий дощ може округлитись до 0 мм, але код погоди його покаже.</summary>
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
        var forecasts = await GetHazardForecastsAsync(new[] { new RoutePoint(lat, lng) }, ct);
        return forecasts[0];
    }

    /// <summary>Прогноз небезпеки для списку точок одним запитом; порядок результатів збігається з порядком точок.</summary>
    public async Task<IReadOnlyList<WeatherForecast>> GetHazardForecastsAsync(
        IReadOnlyList<RoutePoint> points, CancellationToken ct = default)
    {
        if (points.Count == 0) return Array.Empty<WeatherForecast>();

        var inv = System.Globalization.CultureInfo.InvariantCulture;
        var lats = string.Join(",", points.Select(p => p.Lat.ToString(inv)));
        var lngs = string.Join(",", points.Select(p => p.Lng.ToString(inv)));
        var url = $"https://api.open-meteo.com/v1/forecast?latitude={lats}&longitude={lngs}" +
                  "&current=precipitation,temperature_2m,weather_code,snow_depth&minutely_15=precipitation&forecast_days=1&timezone=auto";

        try
        {
            using var response = await _http.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            // Для однієї точки Open-Meteo повертає об'єкт, для кількох — масив об'єктів.
            var locations = doc.RootElement.ValueKind == JsonValueKind.Array
                ? doc.RootElement.EnumerateArray().ToList()
                : new List<JsonElement> { doc.RootElement };

            if (locations.Count != points.Count) return Unavailable(points.Count);

            return locations.Select(AnalyzeLocation).ToList();
        }
        catch (Exception)
        {
            // Open-Meteo недоступний/мережева помилка — не валимо запит клієнта,
            // повертаємо безпечний дефолт з позначеним джерелом.
            return Unavailable(points.Count);
        }
    }

    private static IReadOnlyList<WeatherForecast> Unavailable(int count) =>
        Enumerable.Repeat(new WeatherForecast("NORMAL", 0, "unavailable", "прогноз недоступний"), count).ToList();

    private static WeatherForecast AnalyzeLocation(JsonElement location)
    {
        var current = location.GetProperty("current");
        // Опади прямо зараз (чи вже мокрий асфальт).
        var currentPrecipitation = current.GetProperty("precipitation").GetDouble();
        var temperature = current.GetProperty("temperature_2m").GetDouble();
        var weatherCode = current.GetProperty("weather_code").GetInt32();
        var currentTime = current.GetProperty("time").GetString();
        var snowDepth = current.TryGetProperty("snow_depth", out var snowDepthProp) ? snowDepthProp.GetDouble() : 0;

        // minutely_15 починається з півночі, тож "найближча година" — це 4 значення від індексу current.time.
        var minutely = location.GetProperty("minutely_15");
        var minutelyTimes = minutely.GetProperty("time").EnumerateArray().Select(t => t.GetString()).ToList();
        var minutelyPrecip = minutely.GetProperty("precipitation").EnumerateArray().Select(v => v.ValueKind == JsonValueKind.Number ? v.GetDouble() : 0).ToList();

        var startIndex = minutelyTimes.IndexOf(currentTime);
        if (startIndex < 0) startIndex = 0;

        var nextHourMax = minutelyPrecip
            .Skip(startIndex)
            .Take(4)
            .DefaultIfEmpty(0)
            .Max();

        var worstCasePrecip = Math.Max(currentPrecipitation, nextHourMax);

        // Асфальт не одразу висихає: дивимось назад по тому самому масиву minutely_15.
        var pastWindowStart = Math.Max(0, startIndex - PastHazardWindowSlots);
        var pastPrecipMax = minutelyPrecip
            .Skip(pastWindowStart)
            .Take(Math.Max(0, startIndex - pastWindowStart))
            .DefaultIfEmpty(0)
            .Max();

        // Досить будь-якої однієї причини, щоб умови вважались небезпечними.
        var reasons = new List<string>();
        if (worstCasePrecip >= HazardThresholdMm) reasons.Add("опади (дощ/мряка)");
        if (FreezingPrecipitationCodes.Contains(weatherCode)) reasons.Add("замерзаючі опади (ожеледиця)");
        if (temperature <= FrostTemperatureC) reasons.Add("низька температура (ризик інею/льоду)");
        if (worstCasePrecip < HazardThresholdMm && ActivePrecipitationCodes.Contains(weatherCode))
            reasons.Add("опади за класифікацією погоди (кількість округлилась до 0мм)");
        if (worstCasePrecip < HazardThresholdMm && pastPrecipMax >= HazardThresholdMm)
            reasons.Add("дорога ще мокра після нещодавніх опадів");
        if (snowDepth >= SnowDepthThresholdM)
            reasons.Add("сніговий покрив на дорозі");

        var hazard = reasons.Count > 0 ? "HIGH" : "NORMAL";
        var reasonText = reasons.Count > 0 ? string.Join(", ", reasons) : "без опадів, дорога суха";

        return new WeatherForecast(hazard, worstCasePrecip, "open-meteo.com", reasonText);
    }
}
