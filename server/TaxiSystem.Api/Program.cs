using Microsoft.EntityFrameworkCore;
using TaxiSystem.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=taxi.db"));

builder.Services.AddControllers();

var app = builder.Build();

// EnsureCreated замість EF-міграцій — свідоме спрощення на час дедлайну практики,
// створює схему БД напряму з моделі. Для еволюції схеми в майбутньому — перейти
// на `dotnet ef migrations`.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.MapControllers();

app.Run();
