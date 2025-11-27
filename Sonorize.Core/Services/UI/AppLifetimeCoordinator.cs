using Sonorize.Core.Services.Audio;

namespace Sonorize.Core.Services.UI;

public class AppLifetimeCoordinator
{
    private readonly IPlayerService _playerService;
    private readonly IUiEventService _uiEventService;

    public AppLifetimeCoordinator(IPlayerService playerService, IUiEventService uiEventService)
    {
        _playerService = playerService;
        _uiEventService = uiEventService;
    }

    public void SubscribeToEvents()
    {
        _playerService.PlaybackStateChanged += OnPlaybackStateChanged;
        _playerService.PlaybackProgressed += OnPlaybackProgressed;
        _playerService.PlaybackModesChanged += OnPlaybackModesChanged;
    }

    private void OnPlaybackStateChanged()
    {
        var state = new
        {
            _playerService.IsPlaying,
            _playerService.CurrentSong
        };

        _uiEventService.SendEvent("playbackStateChanged", state);
    }

    private void OnPlaybackProgressed()
    {
        var progress = new
        {
            _playerService.CurrentTime,
            _playerService.TotalTime,
            _playerService.IsSeeking,
            _playerService.SeekPreviewTime,
        };

        _uiEventService.SendEvent("playbackProgressed", progress);
    }

    private void OnPlaybackModesChanged(bool isShuffle, RepeatMode repeatMode)
    {
        var modes = new
        {
            IsShuffle = isShuffle,
            RepeatMode = repeatMode
        };

        _uiEventService.SendEvent("playbackModesChanged", modes);
    }

    public void UnsubscribeFromEvents()
    {
        _playerService.PlaybackStateChanged -= OnPlaybackStateChanged;
        _playerService.PlaybackProgressed -= OnPlaybackProgressed;
        _playerService.PlaybackModesChanged -= OnPlaybackModesChanged;
    }
}