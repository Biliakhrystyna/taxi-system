using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TaxiSystem.Api.Data;
using TaxiSystem.Api.DTOs;
using TaxiSystem.Api.Hubs;
using TaxiSystem.Api.Models;

namespace TaxiSystem.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IHubContext<OrderHub> _hub;

    public OrdersController(AppDbContext db, IHubContext<OrderHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    [HttpPost]
    public async Task<ActionResult<OrderResponse>> Create(CreateOrderRequest request)
    {
        var passenger = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.PassengerEmail);
        if (passenger is null) return BadRequest("Пасажира не знайдено.");

        var basePrice = request.Zone == "center" ? 120 : 180;
        if (request.CarClass == "econom") basePrice -= 30;
        if (request.CarClass == "lux") basePrice += 100;

        var weatherCoeff = request.IsBadWeather ? (request.SafeRouteApplied ? 1.45 : 1.3) : 1.0;
        var bonus = request.Zone == "outskirts" ? 50 : 0;
        var estimatedCost = (int)Math.Round(basePrice * weatherCoeff + bonus);

        var paymentMethodLabel = request.PaymentMethod == "card" ? "Картка" : "Готівка";
        var initialPaymentStatus = paymentMethodLabel == "Картка" ? "Оплачено карткою" : "Очікує оплати готівкою";

        var order = new Order
        {
            OrderId = "ORD_" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            PassengerEmail = passenger.Email,
            PassengerName = $"{passenger.FirstName} {passenger.LastName}",
            PickupLocation = request.PickupLocation,
            Destination = request.Destination,
            CarClass = request.CarClass,
            EstimatedCost = estimatedCost,
            MotivationBonus = bonus,
            WeatherHazardLevel = request.IsBadWeather ? "HIGH" : "NORMAL",
            CurrentStatus = "waiting",
            Zone = request.Zone,
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

    /// <summary>
    /// Для пасажира — його власне активне замовлення. Для водія — єдине активне
    /// замовлення в системі (успадковано від оригінального прототипу, де був
    /// один глобальний "живий" заказ на весь демо-стенд, а не черга по водіях).
    /// </summary>
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
        o.EstimatedCost, o.MotivationBonus, o.WeatherHazardLevel, o.CurrentStatus, o.Zone, o.SafeRouteApplied,
        o.PaymentMethod, o.PaymentStatus, o.DriverEmail, o.DriverName, o.EndTime);
}
