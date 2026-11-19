using Microsoft.Extensions.Logging;
using Sonorize.Core.Core;
using IF.Lastfm.Core.Objects;
using Sonorize.Core.Settings;

using IF.Lastfm.Core.Api;

namespace Sonorize.Core.Scrobbling;

public class ScrobblingService
{
    private readonly LastfmAuthService _authService;
    private readonly SonorizeSettings _settings;
    private readonly ILogger<ScrobblingService> _logger;

    public ScrobblingService(LastfmAuthService authService, SonorizeSettings settings, ILogger<ScrobblingService> logger)
    {
        _authService = authService;
        _settings = settings;
        _logger = logger;
    }

    public async Task UpdateNowPlayingAsync(Song song)
    {
        if (!_settings.Lastfm.ScrobblingEnabled || song is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(song.Artist) || string.IsNullOrWhiteSpace(song.Title))
        {
            return;
        }

        LastfmClient? client = await _authService.GetAuthenticatedClientAsync();

        if (client is null)
        {
            return;
        }

        try
        {
            Scrobble scrobble = new(
                song.Artist,
                song.Album,
                song.Title,
                DateTimeOffset.Now);

            await client.Track.UpdateNowPlayingAsync(scrobble);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LastFM] UpdateNowPlaying failed for {SongTitle}", song.Title);
        }
    }

    public async Task ScrobbleAsync(Song song, DateTime timePlayed)
    {
        if (!_settings.Lastfm.ScrobblingEnabled || song is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(song.Artist) || string.IsNullOrWhiteSpace(song.Title))
        {
            return;
        }

        LastfmClient? client = await _authService.GetAuthenticatedClientAsync();

        if (client is null)
        {
            return;
        }

        try
        {
            Scrobble scrobble = new(
                song.Artist,
                song.Album,
                song.Title,
                timePlayed);

            // Inflatable.Lastfm marks single scrobbling as obsolete in favor of batches,
            // but for a desktop player, real-time single scrobbling is the correct pattern.
            await client.Track.ScrobbleAsync(scrobble);
            _logger.LogInformation("[LastFM] Scrobbled: {Artist} - {SongTitle}", song.Artist, song.Title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LastFM] Scrobble failed for {SongTitle}", song.Title);
        }
    }
}
