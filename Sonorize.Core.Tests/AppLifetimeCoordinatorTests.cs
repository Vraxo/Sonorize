using NSubstitute;
using Sonorize.Core.Models;
using Sonorize.Core.Services.Audio;
using Sonorize.Core.Services.UI;

namespace Sonorize.Core.Tests;

public class AppLifetimeCoordinatorTests
{
    private readonly IPlayerService _playerService;
    private readonly IUiEventService _uiEventService;
    private readonly AppLifetimeCoordinator _coordinator;

    public AppLifetimeCoordinatorTests()
    {
        _playerService = Substitute.For<IPlayerService>();
        _uiEventService = Substitute.For<IUiEventService>();
        _coordinator = new AppLifetimeCoordinator(_playerService, _uiEventService);

        // It's important to call this so the event handlers we're testing are active.
        _coordinator.SubscribeToEvents();
    }

    [Fact]
    public void OnPlaybackStateChanged_WhenEventFires_SendsCorrectEventToUi()
    {
        // Arrange
        Song testSong = new() { FilePath = "test.mp3", Title = "Test Song" };
        _ = _playerService.IsPlaying.Returns(true);
        _ = _playerService.CurrentSong.Returns(testSong);

        object? capturedPayload = null;
        _uiEventService.SendEvent("playbackStateChanged", Arg.Do<object?>(p => capturedPayload = p));

        // Act
        _playerService.PlaybackStateChanged += Raise.Event<Action>();

        // Assert
        _uiEventService.Received(1).SendEvent("playbackStateChanged", Arg.Any<object?>());
        Assert.NotNull(capturedPayload);

        // Use reflection to check properties of the anonymous type
        Type payloadType = capturedPayload.GetType();
        Assert.Equal(true, payloadType.GetProperty("IsPlaying")?.GetValue(capturedPayload));
        Assert.Equal(testSong, payloadType.GetProperty("CurrentSong")?.GetValue(capturedPayload));
    }

    [Fact]
    public void OnPlaybackProgressed_WhenEventFires_SendsCorrectEventToUi()
    {
        // Arrange
        TimeSpan currentTime = TimeSpan.FromSeconds(30);
        TimeSpan totalTime = TimeSpan.FromSeconds(180);
        _ = _playerService.CurrentTime.Returns(currentTime);
        _ = _playerService.TotalTime.Returns(totalTime);
        _ = _playerService.IsSeeking.Returns(false);
        _ = _playerService.SeekPreviewTime.Returns(TimeSpan.Zero);

        object? capturedPayload = null;
        _uiEventService.SendEvent("playbackProgressed", Arg.Do<object?>(p => capturedPayload = p));

        // Act
        _playerService.PlaybackProgressed += Raise.Event<Action>();

        // Assert
        _uiEventService.Received(1).SendEvent("playbackProgressed", Arg.Any<object?>());
        Assert.NotNull(capturedPayload);

        Type payloadType = capturedPayload.GetType();
        Assert.Equal(currentTime, payloadType.GetProperty("CurrentTime")?.GetValue(capturedPayload));
        Assert.Equal(totalTime, payloadType.GetProperty("TotalTime")?.GetValue(capturedPayload));
        Assert.Equal(false, payloadType.GetProperty("IsSeeking")?.GetValue(capturedPayload));
    }

    [Fact]
    public void OnPlaybackModesChanged_WhenEventFires_SendsCorrectEventToUi()
    {
        // Arrange
        object? capturedPayload = null;
        _uiEventService.SendEvent("playbackModesChanged", Arg.Do<object?>(p => capturedPayload = p));

        // Act
        // Raise the event with the arguments it expects (bool, RepeatMode)
        _playerService.PlaybackModesChanged += Raise.Event<Action<bool, RepeatMode>>(true, RepeatMode.All);

        // Assert
        _uiEventService.Received(1).SendEvent("playbackModesChanged", Arg.Any<object?>());
        Assert.NotNull(capturedPayload);

        Type payloadType = capturedPayload.GetType();
        Assert.Equal(true, payloadType.GetProperty("IsShuffle")?.GetValue(capturedPayload));
        Assert.Equal(RepeatMode.All, payloadType.GetProperty("RepeatMode")?.GetValue(capturedPayload));
    }
}