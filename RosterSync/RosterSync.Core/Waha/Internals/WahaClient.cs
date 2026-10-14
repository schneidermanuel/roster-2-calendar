using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RosterSync.Core.Internals;

namespace RosterSync.Core.Waha.Internals;

public class WahaClient
{
    private HttpClient _httpClient;
    private WahaSettings _settings;
    private readonly ILogger<WahaClient> _logger;

    public WahaClient(IOptions<AuthSettings> settings, ILogger<WahaClient> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
        _settings = settings.Value.Waha;
        _httpClient.BaseAddress = new Uri(_settings.Host);
        _httpClient.DefaultRequestHeaders.Add("X-Api-Key", _settings.ApiKey);
    }

    public async Task SendMessage(string number, string message, CancellationToken cancellationToken)
    {
        var chatId = GetChatId(number);
        var body = new
        {
            chatId = chatId,
            session = "default",
            text = message
        };
        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        _logger.LogInformation("WAHA: sending message to {ChatId} ({Length} chars)", chatId, message.Length);
        var response = await _httpClient.PostAsync("/api/sendText", content, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("WAHA: message to {ChatId} accepted ({Status})", chatId,
                (int)response.StatusCode);
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("WAHA: sendText to {ChatId} failed with {Status}: {Error}", chatId,
                (int)response.StatusCode, error);
            throw new HttpRequestException(
                $"WAHA sendText failed with {(int)response.StatusCode}: {error}");
        }
    }

    private string GetChatId(string number)
    {
        var baseNumber = number.Replace(" ", "").Trim();
        if (baseNumber.StartsWith("+"))
        {
            baseNumber = baseNumber.Substring(1);
        }
        else if (baseNumber.StartsWith("0"))
        {
            baseNumber = $"41{baseNumber.Substring(1)}";
        }

        return $"{baseNumber}@c.us";
    }
}