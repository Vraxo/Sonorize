using Sonorize.Core.Models;
using Sonorize.Core.Services.Audio;
using Sonorize.Core.Settings;

namespace Sonorize.Core.Services.Scrobbling;

public class ScrobbleOrchestrator : IDisposable
{
    private readonly IPlayerService _player;
    private readonly ScrobblingService _scrobblingService;
    private readonly ScrobbleEligibilityService _eligibilityService;
    private readonly SonorizeSettings _settings;

    private Song? _currentTrack;
    private bool _hasScrobbledCurrentTrack;

    public ScrobbleOrchestrator(
        IPlayerService player,
        ScrobblingService scrobblingService,
        ScrobbleEligibilityService eligibilityService,
        SonorizeSettings settings)
    {
        _player = player;
        _scrobblingService = scrobblingService;
        _eligibilityService = eligibilityService;
        _settings = settings;

        _player.PlaybackStateChanged += OnPlaybackStateChanged;
        _player.PlaybackProgressed += OnPlaybackProgressed;
    }

    private void OnPlaybackStateChanged()
    {
        // Track Changed Logic
        if (_player.CurrentSong != _currentTrack)
        {
            _currentTrack = _player.CurrentSong;
            _hasScrobbledCurrentTrack = false;

            if (_currentTrack != null && _settings.Lastfm.ScrobblingEnabled)
            {
                _ = _scrobblingService.UpdateNowPlayingAsync(_currentTrack);
            }
        }
    }

    private void OnPlaybackProgressed()
    {
        if (_currentTrack == null || _hasScrobbledCurrentTrack || !_player.IsPlaying)
        {
            return;
        }

        // Check eligibility on every progress tick (approx 10Hz)
        if (_eligibilityService.ShouldScrobble(_currentTrack, _player.CurrentTime, _settings.Lastfm))
        {
            _hasScrobbledCurrentTrack = true;
            _ = _scrobblingService.ScrobbleAsync(_currentTrack, DateTime.UtcNow);
        }
    }

    public void Dispose()
    {
        _player.PlaybackStateChanged -= OnPlaybackStateChanged;
        _player.PlaybackProgressed -= OnPlaybackProgressed;
    }
}