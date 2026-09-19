using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace TaxiSystem.Api.Services;

public class SendGridEmailClient
{
    private const string SendUrl = "https://api.sendgrid.com/v3/mail/send";

    private readonly HttpClient _http;
    private readonly string? _apiKey;
    private readonly string? _fromEmail;
    private readonly ILogger<SendGridEmailClient> _logger;

    public SendGridEmailClient(HttpClient http, IConfiguration config, ILogger<SendGridEmailClient> logger)
    {
        _http = http;
        _apiKey = config["SendGrid:ApiKey"];
        _fromEmail = config["SendGrid:FromEmail"];
        _logger = logger;
    }

    public async Task<bool> SendVerificationCodeAsync(string toEmail, string code, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey) || string.IsNullOrWhiteSpace(_fromEmail))
        {
            _logger.LogWarning("SendGrid:ApiKey/FromEmail не задано — лист не відправлено.");
            return false;
        }

        var body = new
        {
            personalizations = new[] { new { to = new[] { new { email = toEmail } } } },
            from = new { email = _fromEmail, name = "Система таксі перевезень" },
            subject = "Код підтвердження реєстрації",
            content = new[]
            {
                new
                {
                    type = "text/plain",
                    value = $"Ваш код підтвердження: {code}\n\nКод дійсний 15 хвилин.",
                },
            },
        };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, SendUrl)
            {
                Content = JsonContent.Create(body),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            using var response = await _http.SendAsync(request, ct);
            if (response.IsSuccessStatusCode) return true;

            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("SendGrid відповів {Status}: {Body}", response.StatusCode, errorBody);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не вдалося відправити лист через SendGrid.");
            return false;
        }
    }
}
