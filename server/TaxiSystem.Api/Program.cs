using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TaxiSystem.Api.BackgroundServices;
using TaxiSystem.Api.Data;
using TaxiSystem.Api.Hubs;
using TaxiSystem.Api.Services;

var builder = WebApplication.CreateBuilder(args);

const string ClientCorsPolicy = "ClientDev";
var clientOrigin = builder.Configuration["Client:Origin"] ?? "http://localhost:5173";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")
        ?? throw new InvalidOperationException("Не задано рядок підключення ConnectionStrings:Default (PostgreSQL).")));

// snake_case всюди (REST-відповіді й SignalR-повідомлення) — щоб зберегти той самий
// формат полів (order_id, pickup_location, ...), який клієнт уже використовував
// з Firestore, і не переписувати всі шаблони під camelCase.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    });

builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    });

builder.Services.AddHttpClient<OpenMeteoClient>();
builder.Services.AddHttpClient<OpenRouteServiceClient>();
builder.Services.AddHttpClient<OpenRouteServiceGeocodingClient>();
// Nominatim (запасний геокодер) вимагає ідентифікований User-Agent — без
// нього сервіс може відмовляти в запитах (політика використання OSM).
builder.Services.AddHttpClient<NominatimGeocodingClient>(client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("TaxiSystemPracticum/1.0 (student practicum project)");
});

builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddHostedService<DemandZoneCalculatorService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(ClientCorsPolicy, policy =>
        policy.WithOrigins(clientOrigin)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()); // потрібно для SignalR WebSocket-з'єднання
});

var app = builder.Build();

// Застосовує EF-міграції при старті — потрібно для PostgreSQL-хостингу, де файл
// БД не можна просто "створити з моделі" (EnsureCreated) при кожному деплої:
// схема має еволюціонувати керовано через `dotnet ef migrations`.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseCors(ClientCorsPolicy);
app.MapControllers();
app.MapHub<OrderHub>("/hubs/orders");

app.Run();
