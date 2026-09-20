namespace TaxiSystem.Api.Services;

/// <summary>
/// Опорна точка карти (центр Львова) — дефолт для погоди/геокодування, коли
/// клієнт не передав власні координати.
/// </summary>
public static class MapDefaults
{
    public const double LvivLat = 49.8419;
    public const double LvivLng = 24.0315;
}
