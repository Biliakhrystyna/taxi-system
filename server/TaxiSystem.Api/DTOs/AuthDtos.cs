namespace TaxiSystem.Api.DTOs;

public record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string Phone,
    string Role, // "passenger" | "driver"
    string? LicenseNumber,
    string? DriverCarClass);

/// <summary>Identifier — email або телефон, вхід приймає обидва.</summary>
public record LoginRequest(string Identifier, string Password, string Role);

public record VerifyEmailRequest(string Email, string Code);

public record ResendVerificationRequest(string Email);

public record ForgotPasswordRequest(string Email);

public record ResetPasswordRequest(string Email, string Code, string NewPassword);

public record UserResponse(
    string Email,
    string FirstName,
    string LastName,
    string Phone,
    // Роль, під якою відбувся цей вхід/реєстрація в цьому запиті — не єдина
    // роль акаунту (їх може бути дві), а саме та, яку зараз обрав клієнт.
    string Role,
    string Status,
    bool EmailConfirmed,
    int TotalTrips,
    string? LicenseNumber,
    string? DriverCarClass,
    bool IsPassenger,
    bool IsDriver);
