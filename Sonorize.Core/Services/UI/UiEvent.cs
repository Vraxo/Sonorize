using System.Text.Json.Serialization;

namespace Sonorize.Core.Services.UI;

public class UiEvent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "event";

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("payload")]
    public object? Payload { get; set; }
}