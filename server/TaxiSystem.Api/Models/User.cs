namespace TaxiSystem.Api.Models;


public class User
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;

    
    public bool IsPassenger { get; set; }
    public bool IsDriver { get; set; }

   
    public string Status { get; set; } = "active";

    public bool EmailConfirmed { get; set; }
    public string? VerificationCode { get; set; }
    public DateTime? VerificationCodeExpiresAt { get; set; }

    // Заповнюється лише коли IsDriver == true
    public string? LicenseNumber { get; set; }
    public string? DriverCarClass { get; set; }
}
