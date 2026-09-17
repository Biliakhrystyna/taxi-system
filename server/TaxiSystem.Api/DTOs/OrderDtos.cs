namespace TaxiSystem.Api.DTOs;

public record CreateOrderRequest(
    string PassengerEmail,
    string PickupLocation,
    string Destination,
    string CarClass,
    string PaymentMethod,
    bool IsBadWeather,
    bool SafeRouteApplied,
    // Координати можуть бути відсутні, якщо пасажир увів адресу вручну
    // текстом, не вибравши з автопідказок і не клікнувши на мапі — тоді
    // тариф рахується за розумним дефолтом дистанції (див. OrdersController).
    double? PickupLat = null,
    double? PickupLng = null,
    double? DestinationLat = null,
    double? DestinationLng = null);

public record UpdateOrderStatusRequest(string NewStatus, string? DriverEmail, string? DriverName);

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
    string? EndTime);
