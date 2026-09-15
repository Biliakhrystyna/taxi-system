using System.Net.Http.Json;
using System.Text.Json;

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
    private const string DirectionsUrl = "https://api.openrouteservice.org/v2/directions/driving-car";

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
    /// Повертає маршрут з найменшою кількістю поворотів серед альтернатив ORS,
    /// або null, якщо ключ не задано чи сервіс недоступний — виклик тоді має
    /// впасти на клієнтську симуляцію (routeBuilder.ts).
    /// </summary>
    public async Task<SafeRouteResult?> GetSafestRouteAsync(
        double fromLat, double fromLng, double toLat, double toLng, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogWarning("OpenRouteService:ApiKey не задано — реальна маршрутизація пропущена.");
            return null;
        }

        var body = new
        {
            coordinates = new[] { new[] { fromLng, fromLat }, new[] { toLng, toLat } },
            alternative_routes = new { target_count = 3, weight_factor = 1.6, share_factor = 0.6 },
            instructions = true,
            geometry_format = "geojson",
        };

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

            SafeRouteResult? best = null;

            foreach (var route in doc.RootElement.GetProperty("routes").EnumerateArray())
            {
                var points = ExtractPoints(route);
                if (points.Count == 0) continue;

                var turnCount = CountTurns(route);
                var distance = route.GetProperty("summary").GetProperty("distance").GetDouble();

                if (best is null || turnCount < best.TurnCount)
                {
                    best = new SafeRouteResult(points, turnCount, distance, "openrouteservice");
                }
            }

            return best;
        }
        catch (Exception ex)
        {
            // ORS недоступний / вичерпано ліміт / мережева помилка — не валимо
            // запит клієнта, повертаємось до симуляції на клієнті.
            _logger.LogWarning(ex, "OpenRouteService недоступний, повертаємось до симуляції маршруту.");
            return null;
        }
    }

    private static int CountTurns(JsonElement route)
    {
        if (!route.TryGetProperty("segments", out var segments)) return 0;

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

    private static List<RoutePoint> ExtractPoints(JsonElement route)
    {
        var points = new List<RoutePoint>();
        if (!route.TryGetProperty("geometry", out var geometry)) return points;

        // geometry_format=geojson теоретично може повернути або GeoJSON-об'єкт
        // {"type":"LineString","coordinates":[[lng,lat],...]}, або масив координат
        // напряму — підтримуємо обидва варіанти захисно.
        var coordinates = geometry.ValueKind == JsonValueKind.Array
            ? geometry
            : geometry.TryGetProperty("coordinates", out var coords) ? coords : default;

        if (coordinates.ValueKind != JsonValueKind.Array) return points;

        foreach (var pair in coordinates.EnumerateArray())
        {
            if (pair.GetArrayLength() < 2) continue;
            points.Add(new RoutePoint(Lat: pair[1].GetDouble(), Lng: pair[0].GetDouble()));
        }

        return points;
    }
}
