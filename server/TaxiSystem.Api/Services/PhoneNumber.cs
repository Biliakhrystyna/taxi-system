using System.Text.RegularExpressions;

namespace TaxiSystem.Api.Services;

/// <summary>
/// Нормалізація українських мобільних номерів до формату +380XXXXXXXXX
/// Перша цифра після +380 не буває 0/1/2 — цього досить, щоб відсіяти сміття.
/// </summary>
public static class PhoneNumber
{
    private static readonly Regex NineDigits = new(@"^[3-9]\d{8}$", RegexOptions.Compiled);

    public static bool TryNormalize(string? raw, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(raw)) return false;

        var digits = new string(raw.Where(char.IsDigit).ToArray());

        if (digits.Length == 12 && digits.StartsWith("380"))
        {
            digits = digits[3..];
        }
        else if (digits.Length == 10 && digits.StartsWith("0"))
        {
            digits = digits[1..];
        }

        if (digits.Length != 9 || !NineDigits.IsMatch(digits)) return false;

        normalized = "+380" + digits;
        return true;
    }
}
