namespace TaxiSystem.Api.DTOs;

public record CreateOrderRequest(
    string PassengerEmail,
    string PickupLocation,
    string Destination,
    string CarClass,
    string PaymentMethod,
    bool IsBadWeather,
    bool SafeRouteApplied,
    double? PickupLat = null,
    double? PickupLng = null,
    double? DestinationLat = null,
    double? DestinationLng = null,
    // Коли для цієї пари точок безпечний і стандартний маршрут фактично
    // збігаються (немає реального об'їзду) — надбавка за safe route не
    // застосовується, навіть якщо SafeRouteApplied == true.
    bool SafeRouteMatchesStandard = false);

public record UpdateOrderStatusRequest(string NewStatus, string? DriverEmail, string? DriverName);

public record RateOrderRequest(string PassengerEmail, int Rating);

public record QuoteResponse(int EstimatedCost);

public record OrderResponse(
    string OrderId,
    string PassengerEmail,
    string PassengerName,
    string PickupLocation,
    string Destination,
    string CarClass,
    int EstimatedCost,
    string WeatherHazardLevel,
    string CurrentStatus,
    bool SafeRouteApplied,
    string PaymentMethod,
    string PaymentStatus,
    string? DriverEmail,
    string? DriverName,
    string? EndTime,
    double? PickupLat,
    double? PickupLng,
    double? DestinationLat,
    double? DestinationLng,
    int? Rating);
