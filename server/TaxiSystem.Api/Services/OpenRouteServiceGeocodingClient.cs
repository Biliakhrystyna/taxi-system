using System.Globalization;
using System.Text.Json;

namespace TaxiSystem.Api.Services;

public record GeocodedAddress(string Label, double Lat, double Lng);

/// <summary>
/// Клієнт до Geocoding API OpenRouteService (Pelias): переклад адрес у
/// координати (пошук вулиці для автопідказок) і координат в адреси
/// (зворотне геокодування при кліку на мапі) — заміна ручного вводу
/// "lat, lng" на людський текст адреси.
/// На відміну від Directions API, тут ключ передається як query-параметр
/// api_key, а не в заголовку Authorization.
/// </summary>
public class OpenRouteServiceGeocodingClient
{
    private const string BaseUrl = "https://api.openrouteservice.org/geocode";

    private readonly HttpClient _http;
    private readonly string? _apiKey;
    private readonly ILogger<OpenRouteServiceGeocodingClient> _logger;

    public OpenRouteServiceGeocodingClient(HttpClient http, IConfiguration config, ILogger<OpenRouteServiceGeocodingClient> logger)
    {
        _http = http;
        _apiKey = config["OpenRouteService:ApiKey"];
        _logger = logger;
    }

    /// <summary>Найближча адреса до точки на мапі, або null, якщо нічого не знайдено/сервіс недоступний.</summary>
    public async Task<GeocodedAddress?> ReverseAsync(double lat, double lng, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey)) return null;

        var url = $"{BaseUrl}/reverse?api_key={_apiKey}" +
                  $"&point.lat={lat.ToString(CultureInfo.InvariantCulture)}" +
                  $"&point.lon={lng.ToString(CultureInfo.InvariantCulture)}&size=1";

        var results = await FetchAsync(url, ct);
        return results.Count > 0 ? results[0] : null;
    }

    /// <summary>Автопідказки адрес за текстом пошуку, зі зміщенням у бік Львова.</summary>
    public async Task<List<GeocodedAddress>> SearchAsync(string query, double nearLat, double nearLng, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey) || string.IsNullOrWhiteSpace(query)) return new List<GeocodedAddress>();

        var url = $"{BaseUrl}/autocomplete?api_key={_apiKey}&text={Uri.EscapeDataString(query)}" +
                  $"&boundary.circle.lat={nearLat.ToString(CultureInfo.InvariantCulture)}" +
                  $"&boundary.circle.lon={nearLng.ToString(CultureInfo.InvariantCulture)}" +
                  "&boundary.circle.radius=30&layers=address,street,venue,locality";

        return await FetchAsync(url, ct);
    }

    private async Task<List<GeocodedAddress>> FetchAsync(string url, CancellationToken ct)
    {
        var results = new List<GeocodedAddress>();

        try
        {
            using var response = await _http.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            foreach (var feature in doc.RootElement.GetProperty("features").EnumerateArray())
            {
                var coordinates = feature.GetProperty("geometry").GetProperty("coordinates");
                var label = feature.GetProperty("properties").GetProperty("label").GetString();
                if (string.IsNullOrWhiteSpace(label)) continue;

                results.Add(new GeocodedAddress(label, Lat: coordinates[1].GetDouble(), Lng: coordinates[0].GetDouble()));
            }
        }
        catch (Exception ex)
        {
            // Ліміт вичерпано/ORS недоступний/мережева помилка — повертаємо порожній
            // список, клієнт сам впаде на ручний ввід координат.
            _logger.LogWarning(ex, "OpenRouteService Geocoding недоступний.");
        }

        return results;
    }
}
