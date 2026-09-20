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

    // Пряма відстань коротша за реальну дорогу: коефіцієнт наближає її, коли ORS недоступний.
    private const double RoadDetourFactor = 1.3;

    // Дефолтна відстань, коли немає координат (адреса введена вручну).
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

    /// <summary>Орієнтовна ціна до створення замовлення (та сама формула, що й при створенні).</summary>
    [HttpGet("quote")]
    public async Task<ActionResult<QuoteResponse>> Quote(
        [FromQuery] string carClass,
        [FromQuery] bool isBadWeather,
        [FromQuery] bool safeRouteApplied,
        [FromQuery] bool safeRouteMatchesStandard,
        [FromQuery] double? pickupLat,
        [FromQuery] double? pickupLng,
        [FromQuery] double? destinationLat,
        [FromQuery] double? destinationLng)
    {
        var estimatedCost = await CalculateEstimatedCostAsync(
            carClass, isBadWeather, safeRouteApplied, safeRouteMatchesStandard,
            pickupLat, pickupLng, destinationLat, destinationLng, HttpContext.RequestAborted);

        return Ok(new QuoteResponse(estimatedCost));
    }

    [HttpPost]
    public async Task<ActionResult<OrderResponse>> Create(CreateOrderRequest request)
    {
        var passenger = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.PassengerEmail);
        if (passenger is null) return BadRequest("Пасажира не знайдено.");

        if (await ActiveTripGuard.HasActiveTripAsync(_db, passenger.Email, UserRoles.Driver))
        {
            return Conflict("У вас є активна поїздка в ролі водія — завершіть її, перш ніж замовляти як пасажир.");
        }

        var estimatedCost = await CalculateEstimatedCostAsync(
            request.CarClass, request.IsBadWeather, request.SafeRouteApplied, request.SafeRouteMatchesStandard,
            request.PickupLat, request.PickupLng, request.DestinationLat, request.DestinationLng, HttpContext.RequestAborted);

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
            PickupLat = request.PickupLat,
            PickupLng = request.PickupLng,
            DestinationLat = request.DestinationLat,
            DestinationLng = request.DestinationLng,
            EstimatedCost = estimatedCost,
            WeatherHazardLevel = request.IsBadWeather ? "HIGH" : "NORMAL",
            CurrentStatus = OrderStatus.Waiting,
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
        if (role == UserRoles.Driver)
        {
            // Власний активний рейс водія має пріоритет.
            var ownOrder = await _db.Orders
                .Active()
                .Where(o => o.DriverEmail == email)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (ownOrder is not null) return Ok(ToResponse(ownOrder));

            // Інакше — найстаріше (FIFO) замовлення, що очікує водія, лише його класу авто.
            var driver = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

            var waitingQuery = _db.Orders.Where(o => o.CurrentStatus == OrderStatus.Waiting);
            if (driver?.DriverCarClass is not null)
            {
                waitingQuery = waitingQuery.Where(o => o.CarClass == driver.DriverCarClass);
            }

            var nextOrder = await waitingQuery.OrderBy(o => o.CreatedAt).FirstOrDefaultAsync();
            return Ok(nextOrder is null ? null : ToResponse(nextOrder));
        }

        var passengerOrder = await _db.Orders
            .Active()
            .Where(o => o.PassengerEmail == email)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();

        return Ok(passengerOrder is null ? null : ToResponse(passengerOrder));
    }

    [HttpGet("history")]
    public async Task<ActionResult<List<OrderResponse>>> GetHistory([FromQuery] string email, [FromQuery] string role)
    {
        var query = _db.Orders.Finished();

        query = role == UserRoles.Driver
            ? query.Where(o => o.DriverEmail == email)
            : query.Where(o => o.PassengerEmail == email);

        var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
        return Ok(orders.Select(ToResponse));
    }

    /// <summary>Водій не може прийняти власне замовлення або їхати, поки сам їде пасажиром.</summary>
    private async Task<ActionResult?> CheckDriverCanAcceptAsync(string orderId, string? driverEmail)
    {
        var passengerEmail = await _db.Orders
            .Where(o => o.OrderId == orderId)
            .Select(o => o.PassengerEmail)
            .FirstOrDefaultAsync();

        if (passengerEmail is not null && passengerEmail == driverEmail)
        {
            return Conflict("Не можна прийняти власне замовлення.");
        }

        var isRidingNow = await ActiveTripGuard.HasActiveTripAsync(_db, driverEmail, UserRoles.Passenger);

        return isRidingNow
            ? Conflict("У вас є активна поїздка в ролі пасажира — завершіть її, перш ніж приймати замовлення.")
            : null;
    }

    [HttpPost("{orderId}/status")]
    public async Task<ActionResult<OrderResponse>> UpdateStatus(string orderId, UpdateOrderStatusRequest request)
    {
        if (request.NewStatus == OrderStatus.Accepted)
        {
            var conflict = await CheckDriverCanAcceptAsync(orderId, request.DriverEmail);
            if (conflict is not null) return conflict;

            // Атомарний UPDATE: два водії не можуть одночасно прийняти те саме замовлення.
            var rowsUpdated = await _db.Orders
                .Where(o => o.OrderId == orderId && (o.DriverEmail == null || o.DriverEmail == request.DriverEmail))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(o => o.DriverEmail, request.DriverEmail)
                    .SetProperty(o => o.DriverName, request.DriverName)
                    .SetProperty(o => o.CurrentStatus, request.NewStatus));

            if (rowsUpdated == 0)
            {
                var exists = await _db.Orders.AnyAsync(o => o.OrderId == orderId);
                return exists ? Conflict("Це замовлення вже прийняв інший водій.") : NotFound();
            }

            var acceptedOrder = await _db.Orders.AsNoTracking().FirstAsync(o => o.OrderId == orderId);
            var acceptedResponse = ToResponse(acceptedOrder);
            await _hub.Clients.All.SendAsync("OrderUpdated", acceptedResponse);
            return Ok(acceptedResponse);
        }

        var order = await _db.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
        if (order is null) return NotFound();

        order.CurrentStatus = request.NewStatus;

        if (request.NewStatus == OrderStatus.Completed)
        {
            order.EndTime = DateTime.Now.ToLongTimeString();
            order.PaymentStatus = order.PaymentMethod == "Готівка" ? "Оплачено готівкою (водію)" : "Оплачено карткою";
        }

        await _db.SaveChangesAsync();

        var response = ToResponse(order);
        await _hub.Clients.All.SendAsync("OrderUpdated", response);

        return Ok(response);
    }

   
    [HttpPost("{orderId}/rate")]
    public async Task<ActionResult<OrderResponse>> Rate(string orderId, RateOrderRequest request)
    {
        if (request.Rating is < 1 or > 5)
        {
            return BadRequest("Оцінка має бути від 1 до 5.");
        }

        var order = await _db.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
        if (order is null) return NotFound();

        if (order.PassengerEmail != request.PassengerEmail)
        {
            return Unauthorized("Оцінити поїздку може лише пасажир, який нею скористався.");
        }

        if (order.CurrentStatus != OrderStatus.Completed)
        {
            return BadRequest("Оцінити можна лише завершену поїздку.");
        }

        if (order.Rating is not null)
        {
            return Conflict("Цю поїздку вже оцінено.");
        }

        order.Rating = request.Rating;
        await _db.SaveChangesAsync();

        var response = ToResponse(order);
        await _hub.Clients.All.SendAsync("OrderUpdated", response);

        return Ok(response);
    }

    private static OrderResponse ToResponse(Order o) => new(
        o.OrderId, o.PassengerEmail, o.PassengerName, o.PickupLocation, o.Destination, o.CarClass,
        o.EstimatedCost, o.WeatherHazardLevel, o.CurrentStatus, o.SafeRouteApplied,
        o.PaymentMethod, o.PaymentStatus, o.DriverEmail, o.DriverName, o.EndTime,
        o.PickupLat, o.PickupLng, o.DestinationLat, o.DestinationLng, o.Rating);

    
    private async Task<int> CalculateEstimatedCostAsync(
        string carClass, bool isBadWeather, bool safeRouteApplied, bool safeRouteMatchesStandard,
        double? pickupLat, double? pickupLng, double? destinationLat, double? destinationLng, CancellationToken ct)
    {
        var (baseFare, perKm) = CarClassRates.TryGetValue(carClass, out var rates)
            ? rates
            : CarClassRates["comfort"];

        var distanceKm = await ResolveDistanceKmAsync(pickupLat, pickupLng, destinationLat, destinationLng, ct);

        var basePrice = baseFare + perKm * distanceKm;
        var safeRouteHasRealDetour = safeRouteApplied && !safeRouteMatchesStandard;
        var weatherCoeff = isBadWeather ? (safeRouteHasRealDetour ? 1.45 : 1.3) : 1.0;

        return (int)Math.Max(MinimumFare, Math.Round(basePrice * weatherCoeff));
    }

    /// <summary>Відстань по дорогах через ORS; без координат — дефолт, а без ORS — гаверсинус із коефіцієнтом звивистості.</summary>
    private async Task<double> ResolveDistanceKmAsync(
        double? pickupLat, double? pickupLng, double? destinationLat, double? destinationLng, CancellationToken ct)
    {
        if (pickupLat is not double pLat || pickupLng is not double pLng ||
            destinationLat is not double dLat || destinationLng is not double dLng)
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
