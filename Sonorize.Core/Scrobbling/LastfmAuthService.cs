using Microsoft.Extensions.Logging;
using IF.Lastfm.Core.Api.Helpers;
using Sonorize.Core.Settings;

using IF.Lastfm.Core.Api;

namespace Sonorize.Core.Scrobbling;

public class LastfmAuthService
{
    private readonly ISettingsManager<SonorizeSettings> _settingsManager;
    private readonly SonorizeSettings _settings;
    private readonly ILogger<LastfmAuthService> _logger;

    private readonly string _apiKey;
    private readonly string _apiSecret;

    // Fixed: Explicitly check for the placeholders used in documentation to prevent false positives.
    public bool IsAppIdConfigured =>
        !string.IsNullOrWhiteSpace(_apiKey) &&
        _apiKey != "YOUR_API_KEY" &&
        _apiKey != "YOUR_LASTFM_API_KEY";

    public LastfmAuthService(ISettingsManager<SonorizeSettings> settingsManager, SonorizeSettings settings, ILogger<LastfmAuthService> logger)
    {
        _settingsManager = settingsManager;
        _settings = settings;
        _logger = logger;

        // Load keys via the secure C# partial class pattern
        (_apiKey, _apiSecret) = Secrets.GetLastFmKeys();

        if (IsAppIdConfigured)
        {
            _logger.LogInformation("[LastFM] Keys configured.");
        }
        else
        {
            _logger.LogWarning("[LastFM] Keys missing or placeholders. Scrobbling disabled.");
        }
    }

    public async Task<LastfmClient?> GetAuthenticatedClientAsync()
    {
        if (!IsAppIdConfigured)
        {
            return null;
        }

        if (string.IsNullOrEmpty(_settings.Lastfm.SessionKey))
        {
            return null;
        }

        LastAuth auth = new(_apiKey, _apiSecret);
        auth.LoadSession(new() { Token = _settings.Lastfm.SessionKey });

        return new(auth);
    }

    public async Task<LastfmClient?> AuthenticateWithCredentialsAsync(string password)
    {
        if (!IsAppIdConfigured)
        {
            _logger.LogWarning("[LastFM] API Keys not configured.");
            return null;
        }

        LastAuth auth = new(_apiKey, _apiSecret);

        try
        {
            LastResponse response = await auth.GetSessionTokenAsync(_settings.Lastfm.Username!, password);

            if (response.Success && auth.Authenticated && auth.UserSession is not null)
            {
                _settings.Lastfm.SessionKey = auth.UserSession.Token;
                _settingsManager.Save(_settings);
                _logger.LogInformation("[LastFM] Successfully authenticated as {Username}", _settings.Lastfm.Username);
                return new LastfmClient(auth);
            }

            _logger.LogWarning("[LastFM] Auth response failed: {Status}", response.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LastFM] Auth failed");
        }

        return null;
    }

    public bool IsConfigured()
    {
        return IsAppIdConfigured
            && !string.IsNullOrEmpty(_settings.Lastfm.SessionKey);
    }
}
