using Microsoft.AspNetCore.Mvc;
using TaxiSystem.Api.Services;

namespace TaxiSystem.Api.Controllers;

public record RouteForecastRequest(List<RoutePoint>? Points);

public record RoutePointForecast(double Lat, double Lng, string HazardLevel, double PrecipitationMm, string Source, string Reason);

[ApiController]
[Route("api/weather")]
public class WeatherController : ControllerBase
{
    private const int MaxRoutePoints = 12;

    private readonly OpenMeteoClient _openMeteo;

    public WeatherController(OpenMeteoClient openMeteo)
    {
        _openMeteo = openMeteo;
    }


    [HttpGet("forecast")]
    public async Task<ActionResult<WeatherForecast>> Forecast(
        [FromQuery] double lat = MapDefaults.LvivLat,
        [FromQuery] double lng = MapDefaults.LvivLng)
    {
        var forecast = await _openMeteo.GetHazardForecastAsync(lat, lng, HttpContext.RequestAborted);
        return Ok(forecast);
    }

    /// <summary>
    /// Прогноз небезпеки для точок, відібраних вздовж маршруту (одним запитом
    /// до Open-Meteo). Порядок відповіді збігається з порядком точок.
    /// </summary>
    [HttpPost("route")]
    public async Task<ActionResult<IReadOnlyList<RoutePointForecast>>> RouteForecast([FromBody] RouteForecastRequest request)
    {
        var points = request.Points ?? new List<RoutePoint>();
        if (points.Count == 0 || points.Count > MaxRoutePoints)
        {
            return BadRequest($"Потрібно від 1 до {MaxRoutePoints} точок маршруту.");
        }

        var forecasts = await _openMeteo.GetHazardForecastsAsync(points, HttpContext.RequestAborted);
        var result = points
            .Select((p, i) => new RoutePointForecast(p.Lat, p.Lng, forecasts[i].HazardLevel, forecasts[i].PrecipitationMm, forecasts[i].Source, forecasts[i].Reason))
            .ToList();
        return Ok(result);
    }
}
