using System.Text.RegularExpressions;

namespace TaxiSystem.Api.Services;

/// <summary>
/// Валідація номера водійського посвідчення: українське пластикове —
/// 3 літери + 6 цифр (9 символів), без пробілів/дефісів/крапок. Регістр
/// літер завжди приводиться до верхнього, щоб у базі зберігався один
/// стандартизований формат незалежно від того, як ввів користувач.
/// </summary>
public static class DriverLicenseNumber
{
    private static readonly Regex Pattern = new(@"^[A-ZА-ЯҐЄІЇ]{3}\d{6}$", RegexOptions.Compiled);

    public static bool TryNormalize(string? raw, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(raw)) return false;

        // Пробіли (напр. "BXX 123456", як підказує плейсхолдер у формі)
        // прибираємо перед перевіркою — це єдиний символ, який толеруємо.
        // Дефіси, крапки чи будь-що інше лишається в рядку і провалює regex.
        var candidate = raw.Replace(" ", "").ToUpperInvariant();

        if (!Pattern.IsMatch(candidate)) return false;

        normalized = candidate;
        return true;
    }
}
