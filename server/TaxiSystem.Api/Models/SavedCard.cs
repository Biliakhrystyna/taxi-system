namespace TaxiSystem.Api.Models;

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
