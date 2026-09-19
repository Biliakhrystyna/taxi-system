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
            // Акаунт з таким email уже є — та сама людина може додати другу
            // роль (стати ще й водієм/пасажиром) на той самий обліковий запис
            // замість дубліката, але лише підтвердивши, що це справді вона.
            if (!_passwordHasher.Verify(request.Password, existing.PasswordHash))
            {
                return Conflict("Користувач з таким email вже зареєстрований.");
            }

            var alreadyHasRole = request.Role == "driver" ? existing.IsDriver : existing.IsPassenger;
            if (alreadyHasRole)
            {
                var roleLabel = request.Role == "driver" ? "водія" : "пасажира";
                return Conflict($"У цього акаунту вже є роль {roleLabel}.");
            }

            if (await HasActiveOrderInOtherRoleAsync(existing.Email, request.Role))
            {
                return Conflict(ActiveOrderConflictMessage(request.Role));
            }

            string? existingNormalizedLicense = null;
            if (request.Role == "driver")
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
            return Ok(ToResponse(existing, request.Role));
        }

        if (!PhoneNumber.TryNormalize(request.Phone, out var normalizedPhone))
        {
            return BadRequest("Некоректний номер телефону.");
        }

        string? normalizedLicense = null;
        if (request.Role == "driver")
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
            IsPassenger = request.Role == "passenger",
            IsDriver = request.Role == "driver",
            Status = request.Role == "driver" ? "pending_verification" : "active",
            EmailConfirmed = false,
            VerificationCode = code,
            VerificationCodeExpiresAt = DateTime.UtcNow.Add(CodeLifetime),
            TotalTrips = 0,
            LicenseNumber = normalizedLicense,
            DriverCarClass = request.Role == "driver" ? request.DriverCarClass : null,
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        await _email.SendVerificationCodeAsync(user.Email, code, HttpContext.RequestAborted);

        return Ok(ToResponse(user, request.Role));
    }

    [HttpPost("login")]
    public async Task<ActionResult<UserResponse>> Login(LoginRequest request)
    {
        // Identifier — email або телефон. За наявністю "@" визначаємо, який
        // саме стовпець шукати; телефон нормалізується так само, як при
        // реєстрації, щоб "0671234567"/"+380671234567" знаходили той самий
        // рядок незалежно від того, як саме користувач його вписав зараз.
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

        // Один email тепер може мати обидві ролі одночасно — перевіряємо, чи
        // саме ту роль, під якою намагаються увійти, увімкнено на акаунті.
        var hasRole = request.Role == "driver" ? user.IsDriver : user.IsPassenger;
        if (!hasRole)
        {
            var roleLabel = request.Role == "driver" ? "водія" : "пасажира";
            return Unauthorized($"Ця пошта ще не зареєстрована як {roleLabel}.");
        }

        if (!user.EmailConfirmed)
        {
            // 403, не 401 — клієнту потрібно відрізнити "невірний пароль" від
            // "пароль вірний, але пошта не підтверджена", щоб повести на екран
            // введення коду, а не в глухий кут із загальною помилкою.
            return StatusCode(403, "Пошту ще не підтверджено. Введіть код, надісланий на email.");
        }

        if (await HasActiveOrderInOtherRoleAsync(user.Email, request.Role))
        {
            return Conflict(ActiveOrderConflictMessage(request.Role));
        }

        return Ok(ToResponse(user, request.Role));
    }

    /// <summary>Формат і унікальність номера посвідчення водія: 3 літери + 6
    /// цифр (регістр приводиться до верхнього), і воно не повинно вже
    /// належати іншому акаунту — одне посвідчення не може "возити" двох
    /// різних зареєстрованих водіїв.</summary>
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

    /// <summary>Не можна одночасно бути водієм у поїздці й пасажиром, що
    /// замовляє нову — перш ніж пустити в іншу роль, переконуємось, що в
    /// протилежній ролі немає незавершеного рейсу.</summary>
    private Task<bool> HasActiveOrderInOtherRoleAsync(string email, string requestedRole) =>
        requestedRole == "driver"
            ? _db.Orders.AnyAsync(o => o.PassengerEmail == email && o.CurrentStatus != "completed" && o.CurrentStatus != "cancelled")
            : _db.Orders.AnyAsync(o => o.DriverEmail == email && o.CurrentStatus != "completed" && o.CurrentStatus != "cancelled");

    private static string ActiveOrderConflictMessage(string requestedRole)
    {
        var otherRoleLabel = requestedRole == "driver" ? "пасажира" : "водія";
        var requestedRoleLabel = requestedRole == "driver" ? "водія" : "пасажира";
        return $"У вас є активна поїздка в ролі {otherRoleLabel} — завершіть її, перш ніж заходити як {requestedRoleLabel}.";
    }

    /// <summary>Одноразове підтвердження пошти кодом з листа — після успіху вхід
    /// більше жодного коду не вимагає (EmailConfirmed лишається true назавжди).</summary>
    [HttpPost("verify-email")]
    public async Task<ActionResult<UserResponse>> VerifyEmail(VerifyEmailRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user is null) return NotFound("Користувача не знайдено.");

        if (user.EmailConfirmed)
        {
            return Ok(ToResponse(user));
        }

        if (user.VerificationCode != request.Code || user.VerificationCodeExpiresAt < DateTime.UtcNow)
        {
            return BadRequest("Невірний або прострочений код підтвердження.");
        }

        user.EmailConfirmed = true;
        user.VerificationCode = null;
        user.VerificationCodeExpiresAt = null;
        await _db.SaveChangesAsync();

        return Ok(ToResponse(user));
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

    /// <summary>Той самий одноразовий код, тепер для відновлення пароля.
    /// Навмисно завжди повертає 204 — не розкриває, чи існує такий email
    /// (лист іде лише якщо акаунт справді є), щоб не давати спосіб перебором
    /// з'ясовувати зареєстровані адреси.</summary>
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
        // Успішний код на цю пошту — та сама доказовість, що й при реєстрації,
        // тож заразом підтверджуємо email, якщо він ще не був підтверджений.
        user.EmailConfirmed = true;
        user.VerificationCode = null;
        user.VerificationCodeExpiresAt = null;
        await _db.SaveChangesAsync();

        return Ok(ToResponse(user));
    }

    /// <summary>Викликається після проходження "верифікації водія" на клієнті.</summary>
    [HttpPost("verify-driver/{email}")]
    public async Task<ActionResult<UserResponse>> VerifyDriver(string email)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsDriver);
        if (user is null) return NotFound();

        user.Status = "active";
        await _db.SaveChangesAsync();

        return Ok(ToResponse(user, "driver"));
    }

    /// <summary>`role` — під якою роллю відповідь трактується на клієнті цього
    /// запиту (сесія може оперувати лише однією роллю за раз, навіть якщо в
    /// акаунту ввімкнені обидві). Без явного значення — найкращий здогад:
    /// водій, якщо ця роль є, інакше пасажир.</summary>
    private static UserResponse ToResponse(User u, string? role = null) => new(
        u.Email, u.FirstName, u.LastName, u.Phone,
        role ?? (u.IsDriver ? "driver" : "passenger"),
        u.Status, u.EmailConfirmed, u.TotalTrips, u.LicenseNumber, u.DriverCarClass, u.IsPassenger, u.IsDriver);
}
