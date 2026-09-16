namespace TaxiSystem.Api.Models;

/// <summary>
/// Демо-картка, "запам'ятована" для зручності — НЕ реальна платіжна
/// інтеграція. Зберігається лише замаскований номер (останні 4 цифри) і
/// термін дії; повний номер і CVV на сервер взагалі не потрапляють у
/// незамаскованому вигляді довше, ніж триває один запит.
/// </summary>
public class SavedCard
{
    public int Id { get; set; }

    public string OwnerEmail { get; set; } = string.Empty;

    /// <summary>Напр. "**** **** **** 4242".</summary>
    public string MaskedNumber { get; set; } = string.Empty;

    /// <summary>"MM/YY".</summary>
    public string Expiry { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
