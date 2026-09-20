using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TaxiSystem.Api.BackgroundServices;
using TaxiSystem.Api.Data;
using TaxiSystem.Api.Hubs;
using TaxiSystem.Api.Services;

var builder = WebApplication.CreateBuilder(args);

const string ClientCorsPolicy = "ClientDev";
// Кілька origin-ів розділяються комою (напр. локальний і хостинговий клієнт).
var clientOrigins = (builder.Configuration["Client:Origin"] ?? "http://localhost:5173")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

// Хостинг (Railway) віддає порт у змінній PORT.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// DATABASE_URL (postgresql://user:pass@host:port/db) має пріоритет над ConnectionStrings:Default.
var connectionString = ConnectionStringFromDatabaseUrl(Environment.GetEnvironmentVariable("DATABASE_URL"))
    ?? builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Не задано рядок підключення ConnectionStrings:Default або DATABASE_URL (PostgreSQL).");

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));


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

builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<OpenMeteoClient>();
builder.Services.AddHttpClient<OpenRouteServiceClient>();
builder.Services.AddScoped<FareCalculator>();
builder.Services.AddHttpClient<OpenRouteServiceGeocodingClient>();
builder.Services.AddHttpClient<SendGridEmailClient>();
// Nominatim (запасний геокодер) вимагає ідентифікований User-Agent — без
// нього сервіс може відмовляти в запитах (політика використання OSM).
builder.Services.AddHttpClient<NominatimGeocodingClient>(client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("TaxiSystemPracticum/1.0 (student practicum project)");
});

builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddHostedService<WeatherMonitorService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(ClientCorsPolicy, policy =>
        policy.WithOrigins(clientOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()); // потрібно для SignalR WebSocket-з'єднання
});

var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseCors(ClientCorsPolicy);
app.MapControllers();
app.MapHub<OrderHub>("/hubs/orders");

app.Run();

static string? ConnectionStringFromDatabaseUrl(string? databaseUrl)
{
    if (string.IsNullOrWhiteSpace(databaseUrl))
    {
        return null;
    }

    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':', 2);
    var builder = new Npgsql.NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.Port > 0 ? uri.Port : 5432,
        Database = uri.AbsolutePath.TrimStart('/'),
        Username = Uri.UnescapeDataString(userInfo[0]),
        Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : null,
        SslMode = Npgsql.SslMode.Prefer,
    };
    return builder.ConnectionString;
}
