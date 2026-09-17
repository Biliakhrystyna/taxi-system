using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TaxiSystem.Api.Data;
using TaxiSystem.Api.DTOs;
using TaxiSystem.Api.Hubs;
using TaxiSystem.Api.Models;
using TaxiSystem.Api.Services;

namespace TaxiSystem.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    // Посадка (грн) + ціна за кілометр (грн/км) для кожного класу авто.
    private static readonly Dictionary<string, (double BaseFare, double PerKm)> CarClassRates = new()
    {
        ["econom"] = (30, 9),
        ["comfort"] = (50, 12),
        ["lux"] = (100, 20),
    };

    private const double MinimumFare = 50;

    // Пряма відстань завжди коротша за реальну дорогу — цей
    // коефіцієнт наближає її до типової міської дороги, коли ORS недоступний
    // (вичерпана квота/мережева помилка) і реальну відстань дізнатись нізвідки.
    private const double RoadDetourFactor = 1.3;

    // Розумний дефолт, коли немає навіть координат (адреса введена вручну
    // текстом, без вибору з автопідказок чи кліку на мапі) — типова поїздка в межах міста.
    private const double FallbackDistanceKm = 3.0;

    private readonly AppDbContext _db;
    private readonly IHubContext<OrderHub> _hub;
    private readonly OpenRouteServiceClient _ors;

    public OrdersController(AppDbContext db, IHubContext<OrderHub> hub, OpenRouteServiceClient ors)
    {
        _db = db;
        _hub = hub;
        _ors = ors;
    }

    [HttpPost]
    public async Task<ActionResult<OrderResponse>> Create(CreateOrderRequest request)
    {
        var passenger = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.PassengerEmail);
        if (passenger is null) return BadRequest("Пасажира не знайдено.");

        var (baseFare, perKm) = CarClassRates.TryGetValue(request.CarClass, out var rates)
            ? rates
            : CarClassRates["comfort"];

        var distanceKm = await ResolveDistanceKmAsync(request, HttpContext.RequestAborted);

        var basePrice = baseFare + perKm * distanceKm;
        var weatherCoeff = request.IsBadWeather ? (request.SafeRouteApplied ? 1.45 : 1.3) : 1.0;
        var estimatedCost = (int)Math.Max(MinimumFare, Math.Round(basePrice * weatherCoeff));

        var paymentMethodLabel = request.PaymentMethod == "card" ? "Картка" : "Готівка";

        var initialPaymentStatus = paymentMethodLabel == "Картка" ? "Заброньовано карткою" : "Очікує оплати готівкою";

        var order = new Order
        {
            OrderId = "ORD_" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            PassengerEmail = passenger.Email,
            PassengerName = $"{passenger.FirstName} {passenger.LastName}",
            PickupLocation = request.PickupLocation,
            Destination = request.Destination,
            CarClass = request.CarClass,
            EstimatedCost = estimatedCost,
            WeatherHazardLevel = request.IsBadWeather ? "HIGH" : "NORMAL",
            CurrentStatus = "waiting",
            SafeRouteApplied = request.SafeRouteApplied,
            PaymentMethod = paymentMethodLabel,
            PaymentStatus = initialPaymentStatus,
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        var response = ToResponse(order);
        await _hub.Clients.All.SendAsync("OrderUpdated", response);

        return Ok(response);
    }

   
    [HttpGet("current")]
    public async Task<ActionResult<OrderResponse?>> GetCurrent([FromQuery] string email, [FromQuery] string role)
    {
        var query = _db.Orders.Where(o => o.CurrentStatus != "completed" && o.CurrentStatus != "cancelled");

        query = role == "driver"
            ? query
            : query.Where(o => o.PassengerEmail == email);

        var order = await query.OrderByDescending(o => o.CreatedAt).FirstOrDefaultAsync();
        return Ok(order is null ? null : ToResponse(order));
    }

    [HttpGet("history")]
    public async Task<ActionResult<List<OrderResponse>>> GetHistory([FromQuery] string email, [FromQuery] string role)
    {
        var query = _db.Orders.Where(o => o.CurrentStatus == "completed" || o.CurrentStatus == "cancelled");

        query = role == "driver"
            ? query.Where(o => o.DriverEmail == email)
            : query.Where(o => o.PassengerEmail == email);

        var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
        return Ok(orders.Select(ToResponse));
    }

    [HttpPost("{orderId}/status")]
    public async Task<ActionResult<OrderResponse>> UpdateStatus(string orderId, UpdateOrderStatusRequest request)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
        if (order is null) return NotFound();

        order.CurrentStatus = request.NewStatus;

        if (request.NewStatus == "accepted")
        {
            order.DriverEmail = request.DriverEmail;
            order.DriverName = request.DriverName;
        }

        if (request.NewStatus == "completed")
        {
            order.EndTime = DateTime.Now.ToLongTimeString();
            order.PaymentStatus = order.PaymentMethod == "Готівка" ? "Оплачено готівкою (водію)" : "Оплачено карткою";

            var passenger = await _db.Users.FirstOrDefaultAsync(u => u.Email == order.PassengerEmail);
            if (passenger is not null) passenger.TotalTrips += 1;

            if (order.DriverEmail is not null)
            {
                var driver = await _db.Users.FirstOrDefaultAsync(u => u.Email == order.DriverEmail);
                if (driver is not null) driver.TotalTrips += 1;
            }
        }

        await _db.SaveChangesAsync();

        var response = ToResponse(order);
        await _hub.Clients.All.SendAsync("OrderUpdated", response);

        return Ok(response);
    }

    private static OrderResponse ToResponse(Order o) => new(
        o.OrderId, o.PassengerEmail, o.PassengerName, o.PickupLocation, o.Destination, o.CarClass,
        o.EstimatedCost, o.WeatherHazardLevel, o.CurrentStatus, o.SafeRouteApplied,
        o.PaymentMethod, o.PaymentStatus, o.DriverEmail, o.DriverName, o.EndTime);

    /// <summary>
    /// Реальна відстань по дорогах через ORS. Якщо координат немає (ручний
    /// ввід адреси текстом) — розумний дефолт. Якщо координати є, але ORS
    /// недоступний (вичерпана квота/мережева помилка) — не валимо замовлення
    /// фіксованою ціною, а рахуємо наближену відстань по прямій між точками
    /// (гаверсинова формула) із коефіцієнтом на типову звивистість міської
    /// дороги — це набагато чесніше за фіксовану ціну незалежно від маршруту.
    /// </summary>
    private async Task<double> ResolveDistanceKmAsync(CreateOrderRequest request, CancellationToken ct)
    {
        if (request.PickupLat is not double pLat || request.PickupLng is not double pLng ||
            request.DestinationLat is not double dLat || request.DestinationLng is not double dLng)
        {
            return FallbackDistanceKm;
        }

        var route = await _ors.GetFastestRouteAsync(pLat, pLng, dLat, dLng, ct);
        if (route is not null)
        {
            return route.DistanceMeters / 1000.0;
        }

        return HaversineKm(pLat, pLng, dLat, dLng) * RoadDetourFactor;
    }

    private static double HaversineKm(double lat1, double lng1, double lat2, double lng2)
    {
        const double earthRadiusKm = 6371.0;

        double ToRad(double deg) => deg * Math.PI / 180.0;

        var dLat = ToRad(lat2 - lat1);
        var dLng = ToRad(lng2 - lng1);

        var h = Math.Pow(Math.Sin(dLat / 2), 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) * Math.Pow(Math.Sin(dLng / 2), 2);

        return 2 * earthRadiusKm * Math.Asin(Math.Sqrt(h));
    }
}
