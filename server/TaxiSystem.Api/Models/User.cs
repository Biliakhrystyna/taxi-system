namespace TaxiSystem.Api.Models;

/// <summary>
/// Пасажир або водій. Email — природний первинний ключ (як і в оригінальному
/// Firestore-прототипі, де email був id документа) — свідоме спрощення заради
/// швидкості: без surrogate-id клієнту не треба нічого мапити при переході з Firebase.
/// </summary>
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
