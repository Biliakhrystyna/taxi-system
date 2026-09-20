namespace TaxiSystem.Api.Services;

/// <summary>Розрахунок орієнтовної ціни поїздки за класом авто, відстанню й погодою.</summary>
public class FareCalculator
{
    // Посадка (грн) + ціна за кілометр (грн/км) для кожного класу авто.
    private static readonly Dictionary<string, (double BaseFare, double PerKm)> CarClassRates = new()
    {
        ["econom"] = (30, 9),
        ["comfort"] = (50, 12),
        ["lux"] = (100, 20),
    };

    private const double MinimumFare = 50;

    // Пряма відстань коротша за реальну дорогу: коефіцієнт наближає її, коли ORS недоступний.
    private const double RoadDetourFactor = 1.3;

    // Дефолтна відстань, коли немає координат (адреса введена вручну).
    private const double FallbackDistanceKm = 3.0;

    private readonly OpenRouteServiceClient _ors;

    public FareCalculator(OpenRouteServiceClient ors)
    {
        _ors = ors;
    }

    public async Task<int> CalculateAsync(
        string carClass, bool isBadWeather, bool safeRouteApplied, bool safeRouteMatchesStandard,
        double? pickupLat, double? pickupLng, double? destinationLat, double? destinationLng, CancellationToken ct)
    {
        var (baseFare, perKm) = CarClassRates.TryGetValue(carClass, out var rates)
            ? rates
            : CarClassRates["comfort"];

        var distanceKm = await ResolveDistanceKmAsync(pickupLat, pickupLng, destinationLat, destinationLng, ct);

        var basePrice = baseFare + perKm * distanceKm;
        var safeRouteHasRealDetour = safeRouteApplied && !safeRouteMatchesStandard;
        var weatherCoeff = isBadWeather ? (safeRouteHasRealDetour ? 1.45 : 1.3) : 1.0;

        return (int)Math.Max(MinimumFare, Math.Round(basePrice * weatherCoeff));
    }

    /// <summary>Відстань по дорогах через ORS; без координат — дефолт, а без ORS — гаверсинус із коефіцієнтом звивистості.</summary>
    private async Task<double> ResolveDistanceKmAsync(
        double? pickupLat, double? pickupLng, double? destinationLat, double? destinationLng, CancellationToken ct)
    {
        if (pickupLat is not double pLat || pickupLng is not double pLng ||
            destinationLat is not double dLat || destinationLng is not double dLng)
        {
            return FallbackDistanceKm;
        }

        var route = await _ors.GetFastestRouteAsync(pLat, pLng, dLat, dLng, ct);
        if (route is not null)
        {
            return route.DistanceMeters / 1000.0;
        }

        return HaversineKm(pLat, pLng, dLat, dLng) * RoadDetourFactor;
    }

    private static double HaversineKm(double lat1, double lng1, double lat2, double lng2)
    {
        const double earthRadiusKm = 6371.0;

        double ToRad(double deg) => deg * Math.PI / 180.0;

        var dLat = ToRad(lat2 - lat1);
        var dLng = ToRad(lng2 - lng1);

        var h = Math.Pow(Math.Sin(dLat / 2), 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) * Math.Pow(Math.Sin(dLng / 2), 2);

        return 2 * earthRadiusKm * Math.Asin(Math.Sqrt(h));
    }
}
