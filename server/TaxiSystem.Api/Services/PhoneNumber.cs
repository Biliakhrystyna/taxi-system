using System.Text.RegularExpressions;

namespace TaxiSystem.Api.Services;

/// <summary>
/// Нормалізація й валідація українських мобільних номерів до єдиного формату
/// зберігання (+380XXXXXXXXX). Приймає на вході "0671234567", "380671234567"
/// чи "+380671234567" — усі три природні способи введення зводяться до
/// одного канонічного вигляду, інакше пошук за телефоном при вході (Login)
/// був би ненадійним. Перша з 9 цифр після коду країни ніколи не буває 0/1/2
/// в реальних операторських префіксах — цього одного факту досить, щоб
/// відсіяти сміття на кшталт "000000000", не тримаючи вручну список кодів
/// операторів (він періодично змінюється й не вартий такого ризику).
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
