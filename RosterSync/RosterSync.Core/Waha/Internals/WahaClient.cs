using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RosterSync.Core.Internals;

namespace RosterSync.Core.Waha.Internals;

public class WahaClient
{
    private HttpClient _httpClient;
    private WahaSettings _settings;

    public WahaClient(IOptions<AuthSettings> settings)
    {
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
        await _httpClient.PostAsync("/api/sendText", content, cancellationToken);
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