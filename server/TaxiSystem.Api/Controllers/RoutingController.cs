using Microsoft.AspNetCore.Mvc;
using TaxiSystem.Api.Services;

namespace TaxiSystem.Api.Controllers;

[ApiController]
[Route("api/routing")]
public class RoutingController : ControllerBase
{
    private readonly OpenRouteServiceClient _ors;

    public RoutingController(OpenRouteServiceClient ors)
    {
        _ors = ors;
    }

    /// <summary>
    /// Реальний маршрут по дорогах з мінімумом поворотів (для дощу/поганої погоди).
    /// Повертає null, якщо ORS недоступний/без ключа — клієнт тоді сам падає на
    /// симуляцію (routeBuilder.ts:buildSimulatedSafeRoute).
    /// </summary>
    [HttpGet("safe-route")]
    public async Task<ActionResult<SafeRouteResult?>> GetSafeRoute(
        [FromQuery] double fromLat, [FromQuery] double fromLng,
        [FromQuery] double toLat, [FromQuery] double toLng)
    {
        var result = await _ors.GetSafestRouteAsync(fromLat, fromLng, toLat, toLng, HttpContext.RequestAborted);
        return Ok(result);
    }
}
