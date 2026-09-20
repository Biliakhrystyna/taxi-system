using System.Net.Http.Json;
using System.Text.Json;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;

namespace TaxiSystem.Api.Services;

public record RoutePoint(double Lat, double Lng);

public record SafeRouteResult(IReadOnlyList<RoutePoint> Points, int TurnCount, double DistanceMeters, string Source);

/// <summary>
/// Клієнт OpenRouteService Directions API: серед альтернативних маршрутів по справжніх дорогах
/// обирає той, де найменше поворотів (менше маневрів на мокрій дорозі).
/// </summary>
public class OpenRouteServiceClient
{
    // /geojson-варіант ендпоінту повертає геометрію одразу як GeoJSON LineString.
    private const string DirectionsUrl = "https://api.openrouteservice.org/v2/directions/driving-car/geojson";

    /// <summary>Типи кроків ORS, що рахуються поворотом (Left/Right/Sharp*/Roundabout/UTurn); рахуємо за типом кроку, а не за кутом геометрії.</summary>
    private static readonly HashSet<int> TurnStepTypes = new() { 0, 1, 2, 3, 7, 8, 9 };

    /// <summary>Радіус (м) пошуку найближчої дороги до точки (типово 350 м).</summary>
    private const int SnapRadiusMeters = 1000;

    /// <summary>Час зберігання відповіді в кеші: той самий маршрут запитується кілька разів, а квота ORS обмежена.</summary>
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private readonly string? _apiKey;
    private readonly ILogger<OpenRouteServiceClient> _logger;

    public OpenRouteServiceClient(HttpClient http, IMemoryCache cache, IConfiguration config, ILogger<OpenRouteServiceClient> logger)
    {
        _http = http;
        _cache = cache;
        _apiKey = config["OpenRouteService:ApiKey"];
        _logger = logger;
    }

    private static string CacheKey(string kind, double fromLat, double fromLng, double toLat, double toLng) =>
        FormattableString.Invariant($"ors:{kind}:{fromLat:F5},{fromLng:F5}>{toLat:F5},{toLng:F5}");

    /// <summary>Стандартний (найшвидший) маршрут по справжніх дорогах.</summary>
    public Task<SafeRouteResult?> GetFastestRouteAsync(
        double fromLat, double fromLng, double toLat, double toLng, CancellationToken ct = default)
        => RequestSingleRouteAsync(fromLat, fromLng, toLat, toLng, ct);

    /// <summary>Маршрут з найменшою кількістю поворотів; не гірший за найшвидший (ORS не завжди повертає його серед альтернатив).</summary>
    public async Task<SafeRouteResult?> GetSafestRouteAsync(
        double fromLat, double fromLng, double toLat, double toLng, CancellationToken ct = default)
    {
        var candidates = await RequestAlternativesAsync(fromLat, fromLng, toLat, toLng, ct);

        var baseline = await RequestSingleRouteAsync(fromLat, fromLng, toLat, toLng, ct);
        if (baseline is not null) candidates.Add(baseline);

        if (candidates.Count == 0) return null;

        return candidates
            .OrderBy(r => r.TurnCount)
            .ThenBy(r => r.DistanceMeters)
            .First();
    }

    /// <summary>Повертає null, якщо ключ не задано чи сервіс недоступний.</summary>
    private async Task<SafeRouteResult?> RequestSingleRouteAsync(
        double fromLat, double fromLng, double toLat, double toLng, CancellationToken ct)
    {
        var key = CacheKey("single", fromLat, fromLng, toLat, toLng);
        if (_cache.TryGetValue(key, out SafeRouteResult? cached)) return cached;

        var body = new
        {
            coordinates = new[] { new[] { fromLng, fromLat }, new[] { toLng, toLat } },
            radiuses = new[] { SnapRadiusMeters, SnapRadiusMeters },
            instructions = true,
        };

        var features = await SendDirectionsRequestAsync(body, ct);
        var result = features.Count > 0 ? features[0] : null;
        if (result is not null) _cache.Set(key, result, CacheDuration);
        return result;
    }

    /// <summary>Порожній список, якщо ключ не задано чи сервіс недоступний.</summary>
    private async Task<List<SafeRouteResult>> RequestAlternativesAsync(
        double fromLat, double fromLng, double toLat, double toLng, CancellationToken ct)
    {
        var key = CacheKey("alt", fromLat, fromLng, toLat, toLng);
        if (_cache.TryGetValue(key, out List<SafeRouteResult>? cached) && cached is not null) return new List<SafeRouteResult>(cached);

        var body = new
        {
            coordinates = new[] { new[] { fromLng, fromLat }, new[] { toLng, toLat } },
            radiuses = new[] { SnapRadiusMeters, SnapRadiusMeters },
            alternative_routes = new { target_count = 3, weight_factor = 1.6, share_factor = 0.6 },
            instructions = true,
        };

        var results = await SendDirectionsRequestAsync(body, ct);
        if (results.Count > 0) _cache.Set(key, new List<SafeRouteResult>(results), CacheDuration);
        return results;
    }

    private async Task<List<SafeRouteResult>> SendDirectionsRequestAsync(object body, CancellationToken ct)
    {
        var results = new List<SafeRouteResult>();

        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogWarning("OpenRouteService:ApiKey не задано — реальна маршрутизація пропущена.");
            return results;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, DirectionsUrl)
            {
                Content = JsonContent.Create(body),
            };
            // ORS v2 приймає ключ напряму в Authorization, без схеми "Bearer".
            request.Headers.TryAddWithoutValidation("Authorization", _apiKey);

            using var response = await _http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("OpenRouteService відповів {Status}: {Body}", (int)response.StatusCode, errorBody);
                return results;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            // /geojson-відповідь: кожна альтернатива — окремий Feature.
            foreach (var feature in doc.RootElement.GetProperty("features").EnumerateArray())
            {
                var points = ExtractPoints(feature);
                if (points.Count == 0) continue;

                var properties = feature.GetProperty("properties");
                var turnCount = CountTurns(properties);
                var distance = properties.GetProperty("summary").GetProperty("distance").GetDouble();

                results.Add(new SafeRouteResult(points, turnCount, distance, "openrouteservice"));
            }
        }
        catch (Exception ex)
        {
            // ORS недоступний / вичерпано ліміт / мережева помилка — не валимо
            // запит клієнта, повертаємо порожній результат (клієнт покаже пряму лінію).
            _logger.LogWarning(ex, "OpenRouteService недоступний, маршрут не побудовано.");
        }

        return results;
    }

    private static int CountTurns(JsonElement properties)
    {
        if (!properties.TryGetProperty("segments", out var segments)) return 0;

        var turns = 0;
        foreach (var segment in segments.EnumerateArray())
        {
            if (!segment.TryGetProperty("steps", out var steps)) continue;

            foreach (var step in steps.EnumerateArray())
            {
                if (step.TryGetProperty("type", out var typeProp) && TurnStepTypes.Contains(typeProp.GetInt32()))
                {
                    turns++;
                }
            }
        }

        return turns;
    }

    private static List<RoutePoint> ExtractPoints(JsonElement feature)
    {
        var points = new List<RoutePoint>();

        if (!feature.TryGetProperty("geometry", out var geometry)) return points;
        if (!geometry.TryGetProperty("coordinates", out var coordinates)) return points;
        if (coordinates.ValueKind != JsonValueKind.Array) return points;

        foreach (var pair in coordinates.EnumerateArray())
        {
            if (pair.GetArrayLength() < 2) continue;
            points.Add(new RoutePoint(Lat: pair[1].GetDouble(), Lng: pair[0].GetDouble()));
        }

        return points;
    }
}
