namespace TaxiSystem.Api.Models;

/// <summary>
/// Одна таблиця для активних замовлень і завершеної історії — свідоме спрощення
/// (замість окремих Orders + Trips): завершений рядок (CurrentStatus == "completed")
/// просто лишається в тій самій таблиці й відбирається фільтром по статусу.
/// </summary>
public class Order
{
    public string OrderId { get; set; } = string.Empty;

    public string PassengerEmail { get; set; } = string.Empty;
    public string PassengerName { get; set; } = string.Empty;

    public string PickupLocation { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public string CarClass { get; set; } = string.Empty;

    public int EstimatedCost { get; set; }

    /// <summary>"HIGH" | "NORMAL"</summary>
    public string WeatherHazardLevel { get; set; } = "NORMAL";

    /// <summary>"waiting" | "accepted" | "in_progress" | "completed"</summary>
    public string CurrentStatus { get; set; } = "waiting";

    public bool SafeRouteApplied { get; set; }

    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;

    public string? DriverEmail { get; set; }
    public string? DriverName { get; set; }
    public string? EndTime { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
