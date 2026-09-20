using Microsoft.EntityFrameworkCore;
using TaxiSystem.Api.Data;
using TaxiSystem.Api.Models;

namespace TaxiSystem.Api.Services;

/// <summary>Перевірка, чи має користувач незавершену поїздку в заданій ролі.</summary>
public static class ActiveTripGuard
{
    public static Task<bool> HasActiveTripAsync(AppDbContext db, string? email, string role) =>
        role == UserRoles.Driver
            ? db.Orders.Active().AnyAsync(o => o.DriverEmail == email)
            : db.Orders.Active().AnyAsync(o => o.PassengerEmail == email);
}
