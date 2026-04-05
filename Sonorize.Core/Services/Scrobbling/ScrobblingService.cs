using IF.Lastfm.Core.Api;
using IF.Lastfm.Core.Objects;
using Sonorize.Core.Models;
using Sonorize.Core.Services.System;
using Sonorize.Core.Settings;

namespace Sonorize.Core.Services.Scrobbling;

public class ScrobblingService
{
    private readonly LastfmAuthService _authService;
    private readonly SonorizeSettings _settings;
    private readonly LogService _logger;

    public ScrobblingService(LastfmAuthService authService, SonorizeSettings settings, LogService logger)
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
            _logger.Error(
                $"[LastFM] UpdateNowPlaying failed for {song.Title}",
                ex);
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
            _logger.Info($"[LastFM] Scrobbled: {song.Artist} - {song.Title}");
        }
        catch (Exception ex)
        {
            _logger.Error(
                $"[LastFM] Scrobble failed for {song.Title}",
                ex);
        }
    }
}