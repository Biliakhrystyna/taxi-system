using Microsoft.AspNetCore.SignalR;
using TaxiSystem.Api.Services;

namespace TaxiSystem.Api.Hubs;

/// <summary>
/// SignalR-хаб для передачі даних у реальному часі: статус замовлення та
/// зони підвищеного попиту. Клієнти лише слухають push-повідомлення
/// ("OrderUpdated", "ZonesUpdated") — сервер ініціює їх з контролерів
/// (статус замовлення) та з DemandZoneCalculatorService (зони).
/// </summary>
public class OrderHub : Hub
{
    private readonly DemandZoneStore _zoneStore;

    public OrderHub(DemandZoneStore zoneStore)
    {
        _zoneStore = zoneStore;
    }

    /// <summary>Клієнт викликає одразу після підключення, щоб отримати поточні зони
    /// без очікування наступного тику BackgroundService.</summary>
    public async Task RequestCurrentZones()
    {
        await Clients.Caller.SendAsync("ZonesUpdated", _zoneStore.CurrentZones);
    }
}
