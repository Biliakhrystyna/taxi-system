namespace TaxiSystem.Api.DTOs;

public record CreateOrderRequest(
    string PassengerEmail,
    string PickupLocation,
    string Destination,
    string CarClass,
    string PaymentMethod,
    string Zone, // "center" | "outskirts" — визначається клієнтом через geoFilter.ts за зонами з SignalR
    bool IsBadWeather,
    bool SafeRouteApplied);

public record UpdateOrderStatusRequest(string NewStatus, string? DriverEmail, string? DriverName);

public record OrderResponse(
    string OrderId,
    string PassengerEmail,
    string PassengerName,
    string PickupLocation,
    string Destination,
    string CarClass,
    int EstimatedCost,
    int MotivationBonus,
    string WeatherHazardLevel,
    string CurrentStatus,
    string Zone,
    bool SafeRouteApplied,
    string PaymentMethod,
    string PaymentStatus,
    string? DriverEmail,
    string? DriverName,
    string? EndTime);
