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
    private static readonly Random Rng = new();
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(15);

    private readonly AppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly SendGridEmailClient _email;

    public AuthController(AppDbContext db, IPasswordHasher passwordHasher, SendGridEmailClient email)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _email = email;
    }

    [HttpPost("register")]
    public async Task<ActionResult<UserResponse>> Register(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Email і пароль обов'язкові.");
        }

        var existing = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (existing is not null)
        {
       
            if (!_passwordHasher.Verify(request.Password, existing.PasswordHash))
            {
                return Conflict("Користувач з таким email вже зареєстрований.");
            }

            var alreadyHasRole = request.Role == UserRoles.Driver ? existing.IsDriver : existing.IsPassenger;
            if (alreadyHasRole)
            {
                return Conflict($"У цього акаунту вже є роль {UserRoles.Label(request.Role)}.");
            }

            if (await HasActiveOrderInOtherRoleAsync(existing.Email, request.Role))
            {
                return Conflict(ActiveOrderConflictMessage(request.Role));
            }

            string? existingNormalizedLicense = null;
            if (request.Role == UserRoles.Driver)
            {
                var licenseCheck = await ValidateDriverLicenseAsync(request.LicenseNumber, request.Email);
                if (licenseCheck.Error is not null) return licenseCheck.Error;
                existingNormalizedLicense = licenseCheck.Normalized;

                existing.IsDriver = true;
                existing.Status = "pending_verification";
                existing.LicenseNumber = existingNormalizedLicense;
                existing.DriverCarClass = request.DriverCarClass;
            }
            else
            {
                existing.IsPassenger = true;
            }

            await _db.SaveChangesAsync();
            return Ok(await ToResponseAsync(existing, request.Role));
        }

        if (!PhoneNumber.TryNormalize(request.Phone, out var normalizedPhone))
        {
            return BadRequest("Некоректний номер телефону.");
        }

        string? normalizedLicense = null;
        if (request.Role == UserRoles.Driver)
        {
            var licenseCheck = await ValidateDriverLicenseAsync(request.LicenseNumber, request.Email);
            if (licenseCheck.Error is not null) return licenseCheck.Error;
            normalizedLicense = licenseCheck.Normalized;
        }

        var code = Rng.Next(100000, 1000000).ToString();

        var user = new User
        {
            Email = request.Email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = normalizedPhone,
            IsPassenger = request.Role == UserRoles.Passenger,
            IsDriver = request.Role == UserRoles.Driver,
            Status = request.Role == UserRoles.Driver ? "pending_verification" : "active",
            EmailConfirmed = false,
            VerificationCode = code,
            VerificationCodeExpiresAt = DateTime.UtcNow.Add(CodeLifetime),
            LicenseNumber = normalizedLicense,
            DriverCarClass = request.Role == UserRoles.Driver ? request.DriverCarClass : null,
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        await _email.SendVerificationCodeAsync(user.Email, code, HttpContext.RequestAborted);

        return Ok(await ToResponseAsync(user, request.Role));
    }

    [HttpPost("login")]
    public async Task<ActionResult<UserResponse>> Login(LoginRequest request)
    {
        // За наявністю "@" шукаємо за email, інакше за нормалізованим телефоном.
        User? user;
        if (request.Identifier.Contains('@'))
        {
            user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Identifier);
        }
        else if (PhoneNumber.TryNormalize(request.Identifier, out var normalizedIdentifier))
        {
            user = await _db.Users.FirstOrDefaultAsync(u => u.Phone == normalizedIdentifier);
        }
        else
        {
            user = null;
        }

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized("Невірний email/телефон або пароль.");
        }

        // Один email може мати обидві ролі — перевіряємо, що потрібна роль увімкнена.
        var hasRole = request.Role == UserRoles.Driver ? user.IsDriver : user.IsPassenger;
        if (!hasRole)
        {
            return Unauthorized($"Ця пошта ще не зареєстрована як {UserRoles.Label(request.Role)}.");
        }

        if (!user.EmailConfirmed)
        {
           
            return StatusCode(403, "Пошту ще не підтверджено. Введіть код, надісланий на email.");
        }

        if (await HasActiveOrderInOtherRoleAsync(user.Email, request.Role))
        {
            return Conflict(ActiveOrderConflictMessage(request.Role));
        }

        return Ok(await ToResponseAsync(user, request.Role));
    }

    /// <summary>Формат (3 літери + 6 цифр) і унікальність номера посвідчення водія.</summary>
    private async Task<(string? Normalized, ActionResult? Error)> ValidateDriverLicenseAsync(string? rawLicense, string ownerEmail)
    {
        if (!DriverLicenseNumber.TryNormalize(rawLicense, out var normalized))
        {
            return (null, new BadRequestObjectResult("Некоректний номер посвідчення водія. Формат: 3 літери + 6 цифр (напр. ВХХ123456)."));
        }

        var taken = await _db.Users.AnyAsync(u => u.LicenseNumber == normalized && u.Email != ownerEmail);
        if (taken)
        {
            return (null, new ConflictObjectResult("Це посвідчення водія вже зареєстровано на інший акаунт."));
        }

        return (normalized, null);
    }

    /// <summary>Не пускає в іншу роль, поки в протилежній є незавершений рейс.</summary>
    private Task<bool> HasActiveOrderInOtherRoleAsync(string email, string requestedRole) =>
        ActiveTripGuard.HasActiveTripAsync(_db, email, UserRoles.Opposite(requestedRole));

    private static string ActiveOrderConflictMessage(string requestedRole) =>
        $"У вас є активна поїздка в ролі {UserRoles.Label(UserRoles.Opposite(requestedRole))} — завершіть її, перш ніж заходити як {UserRoles.Label(requestedRole)}.";

    /// <summary>Одноразове підтвердження пошти кодом з листа.</summary>
    [HttpPost("verify-email")]
    public async Task<ActionResult<UserResponse>> VerifyEmail(VerifyEmailRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user is null) return NotFound("Користувача не знайдено.");

        if (user.EmailConfirmed)
        {
            return Ok(await ToResponseAsync(user));
        }

        if (user.VerificationCode != request.Code || user.VerificationCodeExpiresAt < DateTime.UtcNow)
        {
            return BadRequest("Невірний або прострочений код підтвердження.");
        }

        user.EmailConfirmed = true;
        user.VerificationCode = null;
        user.VerificationCodeExpiresAt = null;
        await _db.SaveChangesAsync();

        return Ok(await ToResponseAsync(user));
    }

    /// <summary>Новий код, якщо лист не дійшов/код прострочився.</summary>
    [HttpPost("resend-verification")]
    public async Task<ActionResult> ResendVerification(ResendVerificationRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user is null) return NotFound("Користувача не знайдено.");
        if (user.EmailConfirmed) return BadRequest("Пошту вже підтверджено.");

        var code = Rng.Next(100000, 1000000).ToString();
        user.VerificationCode = code;
        user.VerificationCodeExpiresAt = DateTime.UtcNow.Add(CodeLifetime);
        await _db.SaveChangesAsync();

        await _email.SendVerificationCodeAsync(user.Email, code, HttpContext.RequestAborted);

        return NoContent();
    }

 
    [HttpPost("forgot-password")]
    public async Task<ActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user is not null)
        {
            var code = Rng.Next(100000, 1000000).ToString();
            user.VerificationCode = code;
            user.VerificationCodeExpiresAt = DateTime.UtcNow.Add(CodeLifetime);
            await _db.SaveChangesAsync();

            await _email.SendVerificationCodeAsync(user.Email, code, HttpContext.RequestAborted);
        }

        return NoContent();
    }

    [HttpPost("reset-password")]
    public async Task<ActionResult<UserResponse>> ResetPassword(ResetPasswordRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user is null) return NotFound("Користувача не знайдено.");

        if (user.VerificationCode != request.Code || user.VerificationCodeExpiresAt < DateTime.UtcNow)
        {
            return BadRequest("Невірний або прострочений код підтвердження.");
        }

        if (request.NewPassword.Length < 8)
        {
            return BadRequest("Пароль має містити щонайменше 8 символів.");
        }

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        // Успішний код підтверджує й сам email.
        user.EmailConfirmed = true;
        user.VerificationCode = null;
        user.VerificationCodeExpiresAt = null;
        await _db.SaveChangesAsync();

        return Ok(await ToResponseAsync(user));
    }

   
    [HttpPost("verify-driver/{email}")]
    public async Task<ActionResult<UserResponse>> VerifyDriver(string email)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsDriver);
        if (user is null) return NotFound();

        user.Status = "active";
        await _db.SaveChangesAsync();

        return Ok(await ToResponseAsync(user, UserRoles.Driver));
    }

   
    private async Task<UserResponse> ToResponseAsync(User u, string? role = null)
    {
        var effectiveRole = role ?? (u.IsDriver ? UserRoles.Driver : UserRoles.Passenger);

        var totalTrips = effectiveRole == UserRoles.Driver
            ? await _db.Orders.CountAsync(o => o.DriverEmail == u.Email && o.CurrentStatus == OrderStatus.Completed)
            : await _db.Orders.CountAsync(o => o.PassengerEmail == u.Email && o.CurrentStatus == OrderStatus.Completed);

        return new UserResponse(
            u.Email, u.FirstName, u.LastName, u.Phone, effectiveRole,
            u.Status, u.EmailConfirmed, totalTrips, u.LicenseNumber, u.DriverCarClass, u.IsPassenger, u.IsDriver);
    }
}
