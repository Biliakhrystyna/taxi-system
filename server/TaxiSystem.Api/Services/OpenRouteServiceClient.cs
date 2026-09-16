using System.Net.Http.Json;
using System.Text.Json;
using System.Linq;

namespace TaxiSystem.Api.Services;

public record RoutePoint(double Lat, double Lng);

public record SafeRouteResult(IReadOnlyList<RoutePoint> Points, int TurnCount, double DistanceMeters, string Source);

/// <summary>
/// Клієнт до OpenRouteService Directions API. На відміну від
/// buildSimulatedSafeRoute у client/routeBuilder.ts (намальована синусоїда),
/// тут запитуються кілька альтернативних маршрутів по справжніх дорогах
/// (alternative_routes) і обирається той, що має найменше поворотів —
/// менше гальмувань/маневрів на мокрому асфальті, менший ризик підковзування.
/// </summary>
public class OpenRouteServiceClient
{
    // /geojson-варіант ендпоінту: geometry одразу як GeoJSON LineString замість
    // закодованого polyline-рядка (звичайний /v2/directions/driving-car параметра
    // "geometry_format" не приймає — тільки цей окремий шлях).
    private const string DirectionsUrl = "https://api.openrouteservice.org/v2/directions/driving-car/geojson";

    /// <summary>
    /// Типи кроків ORS (`steps[].type`), які вважаємо реальним "поворотом":
    /// Left/Right/SharpLeft/SharpRight/EnterRoundabout/ExitRoundabout/UTurn.
    /// Straight, KeepLeft/Right, Slight*, Depart, Arrive свідомо виключені —
    /// це не маневри, що ризикують підковзуванням на мокрій дорозі.
    /// Рахуємо за типом кроку (семантика ORS), а не за кутом на сирій
    /// геометрії — геометрія містить точки вздовж природного вигину дороги,
    /// що дало б хибно завищену кількість "поворотів".
    /// </summary>
    private static readonly HashSet<int> TurnStepTypes = new() { 0, 1, 2, 3, 7, 8, 9 };

    private readonly HttpClient _http;
    private readonly string? _apiKey;
    private readonly ILogger<OpenRouteServiceClient> _logger;

    public OpenRouteServiceClient(HttpClient http, IConfiguration config, ILogger<OpenRouteServiceClient> logger)
    {
        _http = http;
        _apiKey = config["OpenRouteService:ApiKey"];
        _logger = logger;
    }

    /// <summary>
    /// Стандартний (найшвидший) маршрут по справжніх дорогах — на відміну від
    /// buildDirectRoute() на клієнті (пряма лінія "навпростець" по мапі, що
    /// ігнорує будівлі/квартали/річки). Без запиту альтернатив — ORS сам
    /// повертає єдиний оптимальний маршрут.
    /// </summary>
    public Task<SafeRouteResult?> GetFastestRouteAsync(
        double fromLat, double fromLng, double toLat, double toLng, CancellationToken ct = default)
        => RequestSingleRouteAsync(fromLat, fromLng, toLat, toLng, ct);

    /// <summary>
    /// Маршрут з найменшою кількістю поворотів серед альтернатив ORS (для
    /// мокрої дороги) — на відміну від buildSimulatedSafeRoute() на клієнті
    /// (намальована синусоїда без зв'язку з реальними поворотами).
    /// Гарантовано не гірший за звичайний найшвидший маршрут за кількістю
    /// поворотів: ORS у alternative_routes не завжди повертає той самий
    /// "головний" варіант, що й звичайний запит без alternative_routes (сам
    /// алгоритм пошуку альтернатив може піти іншим шляхом), тож без цієї
    /// гарантії "безпечний" маршрут міг мати БІЛЬШЕ поворотів, ніж стандартний
    /// (виявлено живою перевіркою — 8 проти 5 на реальних координатах).
    /// </summary>
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
        var body = new
        {
            coordinates = new[] { new[] { fromLng, fromLat }, new[] { toLng, toLat } },
            instructions = true,
        };

        var features = await SendDirectionsRequestAsync(body, ct);
        return features.Count > 0 ? features[0] : null;
    }

    /// <summary>Порожній список, якщо ключ не задано чи сервіс недоступний.</summary>
    private async Task<List<SafeRouteResult>> RequestAlternativesAsync(
        double fromLat, double fromLng, double toLat, double toLng, CancellationToken ct)
    {
        var body = new
        {
            coordinates = new[] { new[] { fromLng, fromLat }, new[] { toLng, toLat } },
            alternative_routes = new { target_count = 3, weight_factor = 1.6, share_factor = 0.6 },
            instructions = true,
        };

        return await SendDirectionsRequestAsync(body, ct);
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
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            // /geojson-відповідь — FeatureCollection: кожна альтернатива маршруту
            // це окремий Feature (geometry — LineString напряму на фічі,
            // segments/summary — під properties, а не поряд із geometry).
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
            // запит клієнта, повертаємось до симуляції на клієнті.
            _logger.LogWarning(ex, "OpenRouteService недоступний, повертаємось до симуляції маршруту.");
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

        // feature.geometry — завжди GeoJSON LineString {"type":...,"coordinates":[[lng,lat],...]}
        // на цьому /geojson-ендпоінті.
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
