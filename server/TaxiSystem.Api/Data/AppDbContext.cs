using Microsoft.EntityFrameworkCore;
using TaxiSystem.Api.Models;

namespace TaxiSystem.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasKey(u => u.Email);
        modelBuilder.Entity<Order>().HasKey(o => o.OrderId);
    }
}
