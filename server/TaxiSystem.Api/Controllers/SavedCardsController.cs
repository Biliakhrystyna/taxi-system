using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaxiSystem.Api.Data;
using TaxiSystem.Api.DTOs;
using TaxiSystem.Api.Models;

namespace TaxiSystem.Api.Controllers;

/// <summary>
/// "Збережені картки" пасажира — зручність для повторних замовлень, не
/// реальна платіжна інтеграція. Повний номер картки приходить у тілі запиту
/// лише для того, щоб узяти останні 4 цифри для маски — сервер його ніде
/// не зберігає (ні тут, ні деінде в БД).
/// </summary>
[ApiController]
[Route("api/saved-cards")]
public class SavedCardsController : ControllerBase
{
    private readonly AppDbContext _db;

    public SavedCardsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<SavedCardResponse>>> List([FromQuery] string email)
    {
        var cards = await _db.SavedCards
            .Where(c => c.OwnerEmail == email)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return Ok(cards.Select(ToResponse));
    }

    [HttpPost]
    public async Task<ActionResult<SavedCardResponse>> Add(AddSavedCardRequest request)
    {
        var digits = new string(request.CardNumber.Where(char.IsDigit).ToArray());
        if (digits.Length < 4)
        {
            return BadRequest("Некоректний номер картки.");
        }

        var card = new SavedCard
        {
            OwnerEmail = request.Email,
            MaskedNumber = $"**** **** **** {digits[^4..]}",
            Expiry = request.Expiry,
        };

        _db.SavedCards.Add(card);
        await _db.SaveChangesAsync();

        return Ok(ToResponse(card));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, [FromQuery] string email)
    {
        var card = await _db.SavedCards.FirstOrDefaultAsync(c => c.Id == id && c.OwnerEmail == email);
        if (card is null) return NotFound();

        _db.SavedCards.Remove(card);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private static SavedCardResponse ToResponse(SavedCard c) => new(c.Id, c.MaskedNumber, c.Expiry);
}
