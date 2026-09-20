using Microsoft.AspNetCore.SignalR;

namespace TaxiSystem.Api.Hubs;

/// <summary>
/// SignalR-хаб для передачі даних у реальному часі: статус замовлення та
/// погода. Клієнти лише слухають push-повідомлення ("OrderUpdated",
/// "WeatherUpdated") — сервер ініціює їх з контролерів (статус замовлення)
/// та з WeatherMonitorService (погода).
/// </summary>
public class OrderHub : Hub
{
}
