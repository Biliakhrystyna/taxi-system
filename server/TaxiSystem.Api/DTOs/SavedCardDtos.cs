namespace TaxiSystem.Api.DTOs;

public record SavedCardResponse(int Id, string MaskedNumber, string Expiry);

public record AddSavedCardRequest(string Email, string CardNumber, string Expiry);
