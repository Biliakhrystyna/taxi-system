using System.Text.RegularExpressions;

namespace TaxiSystem.Api.Services;

/// <summary>Валідація посвідчення водія: 3 літери + 6 цифр; регістр приводиться до верхнього.</summary>
public static class DriverLicenseNumber
{
    private static readonly Regex Pattern = new(@"^[A-ZА-ЯҐЄІЇ]{3}\d{6}$", RegexOptions.Compiled);

    public static bool TryNormalize(string? raw, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(raw)) return false;

        // Пробіли прибираємо, інші символи провалюють перевірку.
        var candidate = raw.Replace(" ", "").ToUpperInvariant();

        if (!Pattern.IsMatch(candidate)) return false;

        normalized = candidate;
        return true;
    }
}
