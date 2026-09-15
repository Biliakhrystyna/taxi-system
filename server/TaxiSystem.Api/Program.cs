using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TaxiSystem.Api.Data;
using TaxiSystem.Api.Services;

var builder = WebApplication.CreateBuilder(args);

const string ClientCorsPolicy = "ClientDev";
var clientOrigin = builder.Configuration["Client:Origin"] ?? "http://localhost:5173";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=taxi.db"));

// snake_case всюди (REST-відповіді) — щоб зберегти той самий формат полів
// (order_id, pickup_location, ...), який клієнт уже використовував з Firestore,
// і не переписувати всі шаблони під camelCase.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    });

builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(ClientCorsPolicy, policy =>
        policy.WithOrigins(clientOrigin)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

var app = builder.Build();

// EnsureCreated замість EF-міграцій — свідоме спрощення на час дедлайну практики,
// створює схему БД напряму з моделі. Для еволюції схеми в майбутньому — перейти
// на `dotnet ef migrations`.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseCors(ClientCorsPolicy);
app.MapControllers();

app.Run();
