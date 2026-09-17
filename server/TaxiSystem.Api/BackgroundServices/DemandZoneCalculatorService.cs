using Microsoft.AspNetCore.SignalR;
using TaxiSystem.Api.Hubs;
using TaxiSystem.Api.Services;

namespace TaxiSystem.Api.BackgroundServices;

/// <summary>
/// Фоновий сервіс ASP.NET Core  раз на хвилину опитує Open-Meteo
/// про поточну небезпеку на дорозі для опорної точки й штовхає результат
/// усім підключеним клієнтам через SignalR-хаб OrderHub. Опитування
/// Open-Meteo  —  це періодичний push.
///
/// </summary>
public class DemandZoneCalculatorService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(60);

    private readonly OpenMeteoClient _openMeteo;
    private readonly IHubContext<OrderHub> _hubContext;
    private readonly ILogger<DemandZoneCalculatorService> _logger;

    public DemandZoneCalculatorService(
        OpenMeteoClient openMeteo,
        IHubContext<OrderHub> hubContext,
        ILogger<DemandZoneCalculatorService> logger)
    {
        _openMeteo = openMeteo;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Перший прогноз одразу при старті сервера, щоб не чекати хвилину
        // до першого підключення клієнта.
        await BroadcastWeatherAsync(stoppingToken);

        using var timer = new PeriodicTimer(Interval);
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            await BroadcastWeatherAsync(stoppingToken);
        }
    }

    private async Task BroadcastWeatherAsync(CancellationToken ct)
    {
        var weather = await _openMeteo.GetHazardForecastAsync(MapDefaults.LvivLat, MapDefaults.LvivLng, ct);
        _logger.LogInformation("Погода (опорна точка): {Hazard} — {Reason}", weather.HazardLevel, weather.Reason);
        await _hubContext.Clients.All.SendAsync("WeatherUpdated", weather, ct);
    }
}
