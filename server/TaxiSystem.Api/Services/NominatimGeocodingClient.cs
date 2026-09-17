using System.Globalization;
using System.Text.Json;

namespace TaxiSystem.Api.Services;

/// <summary>
/// Запасний безкоштовний геокодер (OpenStreetMap Nominatim, без ключа) —
/// підключається, лише коли основний ORS Geocoding не дав результату
/// (найчастіше через вичерпану добову квоту безкоштовного ключа ORS —
/// сталось на практиці: {"error":"Quota exceeded"}). Nominatim вимагає
/// ідентифікований User-Agent (див. реєстрацію HttpClient у Program.cs) і не
/// перевіряється власним лімітом квоти нашого ORS-ключа — ідеально як
/// резерв. Використовується лише як fallback, не основне джерело, щоб не
/// перевантажувати чужий безкоштовний сервіс (політика використання
/// Nominatim просить не більше ~1 запиту/сек, що для fallback-сценарію
/// природно виконується).
/// </summary>
public class NominatimGeocodingClient
{
    private readonly HttpClient _http;
    private readonly ILogger<NominatimGeocodingClient> _logger;

    public NominatimGeocodingClient(HttpClient http, ILogger<NominatimGeocodingClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<GeocodedAddress?> ReverseAsync(double lat, double lng, CancellationToken ct = default)
    {
        var url = "https://nominatim.openstreetmap.org/reverse?format=jsonv2" +
                  $"&lat={lat.ToString(CultureInfo.InvariantCulture)}&lon={lng.ToString(CultureInfo.InvariantCulture)}";

        try
        {
            using var response = await _http.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            return ParseFeature(doc.RootElement);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Nominatim (резервний геокодер) теж недоступний.");
            return null;
        }
    }

    public async Task<List<GeocodedAddress>> SearchAsync(
        string query, double? nearLat = null, double? nearLng = null, CancellationToken ct = default)
    {
        var results = new List<GeocodedAddress>();
        if (string.IsNullOrWhiteSpace(query)) return results;

        var url = $"https://nominatim.openstreetmap.org/search?format=jsonv2&limit=10&q={Uri.EscapeDataString(query)}";

        if (nearLat is double lat0 && nearLng is double lng0)
        {
            // viewbox + bounded=0 — те саме м'яке ранжування, що й focus.point в
            // ORS: адреси біля опорної точки пріоритетні, але пошук лишається
            // світовим (bounded=1 жорстко відкидав би все поза межами).
            var box = string.Join(',', new[]
            {
                (lng0 - 0.5).ToString(CultureInfo.InvariantCulture),
                (lat0 + 0.5).ToString(CultureInfo.InvariantCulture),
                (lng0 + 0.5).ToString(CultureInfo.InvariantCulture),
                (lat0 - 0.5).ToString(CultureInfo.InvariantCulture),
            });
            url += $"&viewbox={box}&bounded=0";
        }

        try
        {
            using var response = await _http.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            foreach (var item in doc.RootElement.EnumerateArray())
            {
                var address = ParseFeature(item);
                if (address is not null) results.Add(address);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Nominatim (резервний геокодер) теж недоступний.");
        }

        return results;
    }

    private static GeocodedAddress? ParseFeature(JsonElement item)
    {
        if (!item.TryGetProperty("display_name", out var nameProp)) return null;
        var label = nameProp.GetString();
        if (string.IsNullOrWhiteSpace(label)) return null;

        if (!item.TryGetProperty("lat", out var latProp) || !item.TryGetProperty("lon", out var lonProp)) return null;

        var lat = double.Parse(latProp.GetString()!, CultureInfo.InvariantCulture);
        var lon = double.Parse(lonProp.GetString()!, CultureInfo.InvariantCulture);

        return new GeocodedAddress(label, lat, lon);
    }
}
