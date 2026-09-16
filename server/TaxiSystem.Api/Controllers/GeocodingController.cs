using Microsoft.AspNetCore.Mvc;
using TaxiSystem.Api.Services;

namespace TaxiSystem.Api.Controllers;

[ApiController]
[Route("api/geocoding")]
public class GeocodingController : ControllerBase
{
    private readonly OpenRouteServiceGeocodingClient _geocoding;

    public GeocodingController(OpenRouteServiceGeocodingClient geocoding)
    {
        _geocoding = geocoding;
    }

    /// <summary>Координати → людська адреса (для кліку на мапі).</summary>
    [HttpGet("reverse")]
    public async Task<ActionResult<GeocodedAddress?>> Reverse([FromQuery] double lat, [FromQuery] double lng)
    {
        var result = await _geocoding.ReverseAsync(lat, lng, HttpContext.RequestAborted);
        return Ok(result);
    }

    /// <summary>Автопідказки адрес за введеним текстом (для пошуку вулиці).</summary>
    [HttpGet("search")]
    public async Task<ActionResult<List<GeocodedAddress>>> Search([FromQuery] string query)
    {
        var results = await _geocoding.SearchAsync(query, DemandZoneStore.LvivLat, DemandZoneStore.LvivLng, HttpContext.RequestAborted);
        return Ok(results);
    }
}
