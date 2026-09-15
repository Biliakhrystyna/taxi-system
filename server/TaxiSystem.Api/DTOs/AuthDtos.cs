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

public record LoginRequest(string Email, string Password, string Role);

public record UserResponse(
    string Email,
    string FirstName,
    string LastName,
    string Phone,
    string Role,
    string Status,
    int TotalTrips,
    string? LicenseNumber,
    string? DriverCarClass);
