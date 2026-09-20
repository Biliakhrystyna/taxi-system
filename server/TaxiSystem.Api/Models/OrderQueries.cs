namespace TaxiSystem.Api.Models;

public static class OrderQueries
{
    /// <summary>Замовлення, які ще не завершені й не скасовані.</summary>
    public static IQueryable<Order> Active(this IQueryable<Order> orders) =>
        orders.Where(o => o.CurrentStatus != OrderStatus.Completed && o.CurrentStatus != OrderStatus.Cancelled);

    /// <summary>Завершені або скасовані замовлення — історія поїздок.</summary>
    public static IQueryable<Order> Finished(this IQueryable<Order> orders) =>
        orders.Where(o => o.CurrentStatus == OrderStatus.Completed || o.CurrentStatus == OrderStatus.Cancelled);
}
