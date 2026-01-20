using IF.Lastfm.Core.Api;
using IF.Lastfm.Core.Objects;
using Sonorize.Core.Configuration;
using Sonorize.Core.Services.System;
using Sonorize.Core.Settings;

namespace Sonorize.Core.Services.Scrobbling;

public class LastfmAuthService
{
    private readonly ISettingsManager<SonorizeSettings> _settingsManager;
    private readonly SonorizeSettings _settings;
    private readonly LogService _logger;

    private readonly string _apiKey;
    private readonly string _apiSecret;

    // Fixed: Explicitly check for the placeholders used in documentation to prevent false positives.
    public bool IsAppIdConfigured =>
        !string.IsNullOrWhiteSpace(_apiKey) &&
        _apiKey != "YOUR_API_KEY" &&
        _apiKey != "YOUR_LASTFM_API_KEY";

    public LastfmAuthService(ISettingsManager<SonorizeSettings> settingsManager, SonorizeSettings settings, LogService logger)
    {
        _settingsManager = settingsManager;
        _settings = settings;
        _logger = logger;

        // Load keys via the secure C# partial class pattern
        (_apiKey, _apiSecret) = Secrets.GetLastFmKeys();

        if (IsAppIdConfigured)
        {
            _logger.Info("[LastFM] Keys configured.");
        }
        else
        {
            _logger.Warn("[LastFM] Keys missing or placeholders. Scrobbling disabled.");
        }
    }

    public async Task<LastfmClient?> GetAuthenticatedClientAsync()
    {
        if (!IsAppIdConfigured)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(_settings.Lastfm.SessionKey))
        {
            var auth = new LastAuth(_apiKey, _apiSecret);
            _ = auth.LoadSession(new LastUserSession { Token = _settings.Lastfm.SessionKey });
            return new LastfmClient(auth);
        }

        return null;
    }

    public async Task<LastfmClient?> AuthenticateWithCredentialsAsync(string password)
    {
        if (!IsAppIdConfigured)
        {
            _logger.Warn("[LastFM] API Keys not configured.");
            return null;
        }

        var auth = new LastAuth(_apiKey, _apiSecret);
        try
        {
            var response = await auth.GetSessionTokenAsync(_settings.Lastfm.Username!, password);

            if (response.Success && auth.Authenticated && auth.UserSession is not null)
            {
                _settings.Lastfm.SessionKey = auth.UserSession.Token;
                _settingsManager.Save(_settings);
                _logger.Info($"[LastFM] Successfully authenticated as {_settings.Lastfm.Username}");
                return new LastfmClient(auth);
            }

            _logger.Warn($"[LastFM] Auth response failed: {response.Status}");
        }
        catch (Exception ex)
        {
            _logger.Error("[LastFM] Auth failed", ex);
        }

        return null;
    }

    public bool IsConfigured()
    {
        return IsAppIdConfigured && !string.IsNullOrEmpty(_settings.Lastfm.SessionKey);
    }
}