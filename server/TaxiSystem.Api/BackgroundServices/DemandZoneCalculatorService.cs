using Microsoft.AspNetCore.SignalR;
using TaxiSystem.Api.Hubs;
using TaxiSystem.Api.Services;

namespace TaxiSystem.Api.BackgroundServices;

/// <summary>
/// Фоновий сервіс ASP.NET Core (вимога ТЗ): раз на хвилину перераховує зони
/// підвищеного попиту та штовхає оновлення всім підключеним клієнтам через
/// SignalR-хаб OrderHub. Заміняє клієнтський Math.random() при монтуванні
/// карти на серверний періодичний перерахунок.
/// </summary>
public class DemandZoneCalculatorService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(60);

    private readonly DemandZoneStore _zoneStore;
    private readonly IHubContext<OrderHub> _hubContext;
    private readonly ILogger<DemandZoneCalculatorService> _logger;

    public DemandZoneCalculatorService(
        DemandZoneStore zoneStore,
        IHubContext<OrderHub> hubContext,
        ILogger<DemandZoneCalculatorService> logger)
    {
        _zoneStore = zoneStore;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Перший набір зон одразу при старті сервера, щоб не чекати хвилину
        // до першого підключення клієнта.
        await RecalculateAndBroadcastAsync(stoppingToken);

        using var timer = new PeriodicTimer(Interval);
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RecalculateAndBroadcastAsync(stoppingToken);
        }
    }

    private async Task RecalculateAndBroadcastAsync(CancellationToken ct)
    {
        var zones = _zoneStore.Regenerate();
        _logger.LogInformation("Перераховано {Count} зон попиту", zones.Count);
        await _hubContext.Clients.All.SendAsync("ZonesUpdated", zones, ct);
    }
}
