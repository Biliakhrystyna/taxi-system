namespace TaxiSystem.Api.Services;

/// <summary>
/// Формальна перевірка номера картки (не реальний платіжний процесинг —
/// див. коментар у SavedCardsController.cs): лише цифри/пробіли, рівно
/// 16 цифр, не всі цифри однакові (Луна сам по собі вважає "0000..." чи
/// "1111..." математично коректними — контрольна сума 0 mod 10 = 0), і сама
/// контрольна сума за алгоритмом Луна.
/// </summary>
public static class CardNumber
{
    public static bool IsValid(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return false;
        if (raw.Any(c => !char.IsDigit(c) && !char.IsWhiteSpace(c))) return false;

        var digits = new string(raw.Where(char.IsDigit).ToArray());
        if (digits.Length != 16) return false;
        if (digits.Distinct().Count() == 1) return false;

        return LuhnCheck(digits);
    }

    private static bool LuhnCheck(string digits)
    {
        var sum = 0;
        var shouldDouble = false;

        for (var i = digits.Length - 1; i >= 0; i--)
        {
            var d = digits[i] - '0';
            if (shouldDouble)
            {
                d *= 2;
                if (d > 9) d -= 9;
            }

            sum += d;
            shouldDouble = !shouldDouble;
        }

        return sum % 10 == 0;
    }
}
