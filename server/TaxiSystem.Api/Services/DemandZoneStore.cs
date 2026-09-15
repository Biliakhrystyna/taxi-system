namespace TaxiSystem.Api.Services;

public record DemandZone(string Id, double Lat, double Lng, double Radius);

/// <summary>
/// Тримає поточний набір зон підвищеного попиту в пам'яті сервера й генерує
/// новий набір за запитом. Раніше ця генерація виконувалась на клієнті
/// (TaxiMap.vue, одноразово при монтуванні) — тепер це серверний стан, який
/// DemandZoneCalculatorService (BackgroundService) періодично оновлює,
/// а клієнти отримують через SignalR замість Math.random() при завантаженні карти.
/// </summary>
public class DemandZoneStore
{
    private static readonly Random Rng = new();
    private readonly object _lock = new();
    private IReadOnlyList<DemandZone> _zones = Array.Empty<DemandZone>();

    public const double LvivLat = 49.8419;
    public const double LvivLng = 24.0315;

    public IReadOnlyList<DemandZone> CurrentZones
    {
        get { lock (_lock) return _zones; }
    }

    public IReadOnlyList<DemandZone> Regenerate(int count = 3)
    {
        var zones = new List<DemandZone>(count);

        for (var i = 0; i < count; i++)
        {
            var angle = Rng.NextDouble() * Math.PI * 2;
            var distanceKm = 2.0 + Rng.NextDouble() * 2.5;
            var latOffset = distanceKm / 111.32 * Math.Sin(angle);
            var lngOffset = distanceKm / (111.32 * Math.Cos(LvivLat * Math.PI / 180)) * Math.Cos(angle);
            var radius = 1000 + Rng.NextDouble() * 300;

            zones.Add(new DemandZone(
                Id: $"ZONE_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{i}",
                Lat: LvivLat + latOffset,
                Lng: LvivLng + lngOffset,
                Radius: radius));
        }

        lock (_lock)
        {
            _zones = zones;
        }

        return zones;
    }
}
