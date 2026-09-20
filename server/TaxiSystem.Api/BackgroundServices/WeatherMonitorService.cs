using Microsoft.AspNetCore.SignalR;
using TaxiSystem.Api.Hubs;
using TaxiSystem.Api.Services;

namespace TaxiSystem.Api.BackgroundServices;

/// <summary>
/// Раз на хвилину опитує Open-Meteo для опорної точки й розсилає результат усім клієнтам через SignalR-хаб OrderHub.
/// </summary>
public class WeatherMonitorService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(60);

    private readonly OpenMeteoClient _openMeteo;
    private readonly IHubContext<OrderHub> _hubContext;
    private readonly ILogger<WeatherMonitorService> _logger;

    public WeatherMonitorService(
        OpenMeteoClient openMeteo,
        IHubContext<OrderHub> hubContext,
        ILogger<WeatherMonitorService> logger)
    {
        _openMeteo = openMeteo;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Перший прогноз одразу при старті.
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
