namespace TaxiSystem.Api.Models;


public class User
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;

    /// <summary>"passenger" | "driver"</summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>"active" | "pending_verification"</summary>
    public string Status { get; set; } = "active";

    public int TotalTrips { get; set; }

    // Заповнюється лише для Role == "driver"
    public string? LicenseNumber { get; set; }
    public string? DriverCarClass { get; set; }
}
