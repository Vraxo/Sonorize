using NSubstitute;
using Sonorize.Core.Services.UI;
using System.Text.Json;

namespace Sonorize.Core.Tests;

public class UiEventServiceTests
{
    private readonly IUiBridge _uiBridge;
    private readonly UiEventService _uiEventService;

    public UiEventServiceTests()
    {
        _uiBridge = Substitute.For<IUiBridge>();
        _uiEventService = new UiEventService(_uiBridge);
    }

    [Fact]
    public void SendEvent_WithSimplePayload_SendsCorrectlyFormattedJson()
    {
        // Arrange
        string? capturedJson = null;
        _uiBridge.SendMessage(Arg.Do<string>(json => capturedJson = json));

        // Act
        _uiEventService.SendEvent("testEvent", "Hello World");

        // Assert
        _uiBridge.Received(1).SendMessage(Arg.Any<string>());
        Assert.NotNull(capturedJson);

        using JsonDocument jsonDoc = JsonDocument.Parse(capturedJson);
        JsonElement root = jsonDoc.RootElement;

        Assert.Equal("event", root.GetProperty("type").GetString());
        Assert.Equal("testEvent", root.GetProperty("name").GetString());
        Assert.Equal("Hello World", root.GetProperty("payload").GetString());
    }

    [Fact]
    public void SendEvent_WithComplexPayload_SerializesToCamelCase()
    {
        // Arrange
        string? capturedJson = null;
        _uiBridge.SendMessage(Arg.Do<string>(json => capturedJson = json));
        var payload = new { IsPlaying = true, CurrentSongTitle = "Awesome Song" };

        // Act
        _uiEventService.SendEvent("playbackStateChanged", payload);

        // Assert
        Assert.NotNull(capturedJson);

        using JsonDocument jsonDoc = JsonDocument.Parse(capturedJson);
        JsonElement root = jsonDoc.RootElement;
        JsonElement payloadElement = root.GetProperty("payload");

        // Verify that properties were correctly serialized to camelCase
        Assert.True(payloadElement.GetProperty("isPlaying").GetBoolean());
        Assert.Equal("Awesome Song", payloadElement.GetProperty("currentSongTitle").GetString());
    }

    [Fact]
    public void SendEvent_WithNullPayload_SendsJsonWithNullPayload()
    {
        // Arrange
        string? capturedJson = null;
        _uiBridge.SendMessage(Arg.Do<string>(json => capturedJson = json));

        // Act
        _uiEventService.SendEvent("actionFinished", null);

        // Assert
        Assert.NotNull(capturedJson);

        using JsonDocument jsonDoc = JsonDocument.Parse(capturedJson);
        JsonElement root = jsonDoc.RootElement;

        Assert.Equal(JsonValueKind.Null, root.GetProperty("payload").ValueKind);
    }

    [Fact]
    public void SendEvent_WithEnumPayload_SerializesEnumAsString()
    {
        // Arrange
        string? capturedJson = null;
        _uiBridge.SendMessage(Arg.Do<string>(json => capturedJson = json));
        var payload = new { Mode = RepeatMode.All };

        // Act
        _uiEventService.SendEvent("repeatModeChanged", payload);

        // Assert
        Assert.NotNull(capturedJson);

        using JsonDocument jsonDoc = JsonDocument.Parse(capturedJson);
        JsonElement root = jsonDoc.RootElement;
        JsonElement payloadElement = root.GetProperty("payload");

        Assert.Equal("All", payloadElement.GetProperty("mode").GetString());
    }
}