using Microsoft.AspNetCore.Mvc;
using TaxiSystem.Api.Services;

namespace TaxiSystem.Api.Controllers;

[ApiController]
[Route("api/geocoding")]
public class GeocodingController : ControllerBase
{
    private readonly OpenRouteServiceGeocodingClient _geocoding;
    private readonly NominatimGeocodingClient _fallback;

    public GeocodingController(OpenRouteServiceGeocodingClient geocoding, NominatimGeocodingClient fallback)
    {
        _geocoding = geocoding;
        _fallback = fallback;
    }

    /// <summary>Координати → людська адреса (для кліку на мапі). Якщо ORS не дав
    /// результату (напр. вичерпана добова квота) — пробує Nominatim.</summary>
    [HttpGet("reverse")]
    public async Task<ActionResult<GeocodedAddress?>> Reverse([FromQuery] double lat, [FromQuery] double lng)
    {
        var result = await _geocoding.ReverseAsync(lat, lng, HttpContext.RequestAborted)
                     ?? await _fallback.ReverseAsync(lat, lng, HttpContext.RequestAborted);
        return Ok(result);
    }

    /// <summary>Автопідказки адрес за введеним текстом (для пошуку вулиці). Якщо
    /// ORS повернув порожньо (напр. вичерпана добова квота) — пробує Nominatim.</summary>
    [HttpGet("search")]
    public async Task<ActionResult<List<GeocodedAddress>>> Search([FromQuery] string query)
    {
        var results = await _geocoding.SearchAsync(query, MapDefaults.LvivLat, MapDefaults.LvivLng, HttpContext.RequestAborted);
        if (results.Count == 0)
        {
            results = await _fallback.SearchAsync(
                query, MapDefaults.LvivLat, MapDefaults.LvivLng, HttpContext.RequestAborted);
        }

        return Ok(results);
    }
}
