namespace TaxiSystem.Api.Models;


public class User
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;

    // Один email — одна ідентичність, але дві незалежні ролі можуть бути
    // ввімкнені одночасно (та сама людина замовляє поїздки і возить інших).
    public bool IsPassenger { get; set; }
    public bool IsDriver { get; set; }

    /// <summary>"active" | "pending_verification" — стосується лише ролі водія
    /// (перевірка документів), для пасажирської ролі завжди фактично "active".</summary>
    public string Status { get; set; } = "active";

    public bool EmailConfirmed { get; set; }
    public string? VerificationCode { get; set; }
    public DateTime? VerificationCodeExpiresAt { get; set; }

    public int TotalTrips { get; set; }

    // Заповнюється лише коли IsDriver == true
    public string? LicenseNumber { get; set; }
    public string? DriverCarClass { get; set; }
}
