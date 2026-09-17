using Microsoft.AspNetCore.Mvc;
using TaxiSystem.Api.Services;

namespace TaxiSystem.Api.Controllers;

[ApiController]
[Route("api/weather")]
public class WeatherController : ControllerBase
{
    private readonly OpenMeteoClient _openMeteo;

    public WeatherController(OpenMeteoClient openMeteo)
    {
        _openMeteo = openMeteo;
    }

    /// <summary>Прогноз погоди для Львова (координати за замовчуванням) або довільної точки.</summary>
    [HttpGet("forecast")]
    public async Task<ActionResult<WeatherForecast>> Forecast(
        [FromQuery] double lat = MapDefaults.LvivLat,
        [FromQuery] double lng = MapDefaults.LvivLng)
    {
        var forecast = await _openMeteo.GetHazardForecastAsync(lat, lng, HttpContext.RequestAborted);
        return Ok(forecast);
    }
}
