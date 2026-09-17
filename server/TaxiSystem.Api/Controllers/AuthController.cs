using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaxiSystem.Api.Data;
using TaxiSystem.Api.DTOs;
using TaxiSystem.Api.Models;
using TaxiSystem.Api.Services;

namespace TaxiSystem.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;

    public AuthController(AppDbContext db, IPasswordHasher passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    [HttpPost("register")]
    public async Task<ActionResult<UserResponse>> Register(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Email і пароль обов'язкові.");
        }

        var exists = await _db.Users.AnyAsync(u => u.Email == request.Email);
        if (exists)
        {
            return Conflict("Користувач з таким email вже зареєстрований.");
        }

        var user = new User
        {
            Email = request.Email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = request.Phone,
            Role = request.Role,
            Status = request.Role == "driver" ? "pending_verification" : "active",
            TotalTrips = 0,
            LicenseNumber = request.Role == "driver" ? request.LicenseNumber : null,
            DriverCarClass = request.Role == "driver" ? request.DriverCarClass : null,
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok(ToResponse(user));
    }

    [HttpPost("login")]
    public async Task<ActionResult<UserResponse>> Login(LoginRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email && u.Role == request.Role);
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized("Невірний email або пароль.");
        }

        return Ok(ToResponse(user));
    }

    /// <summary>Викликається після проходження "верифікації водія" на клієнті.</summary>
    [HttpPost("verify-driver/{email}")]
    public async Task<ActionResult<UserResponse>> VerifyDriver(string email)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email && u.Role == "driver");
        if (user is null) return NotFound();

        user.Status = "active";
        await _db.SaveChangesAsync();

        return Ok(ToResponse(user));
    }

    private static UserResponse ToResponse(User u) => new(
        u.Email, u.FirstName, u.LastName, u.Phone, u.Role, u.Status, u.TotalTrips, u.LicenseNumber, u.DriverCarClass);
}
