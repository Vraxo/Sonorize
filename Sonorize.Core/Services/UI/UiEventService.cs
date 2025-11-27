using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sonorize.Core.Services.UI;

public class UiEventService : IUiEventService
{
    private readonly IUiBridge _uiBridge;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public UiEventService(IUiBridge uiBridge)
    {
        _uiBridge = uiBridge;
    }

    public void SendEvent(string eventName, object? payload)
    {
        UiEvent uiEvent = new() { Name = eventName, Payload = payload };
        string json = JsonSerializer.Serialize(uiEvent, _jsonOptions);
        _uiBridge.SendMessage(json);
    }
}