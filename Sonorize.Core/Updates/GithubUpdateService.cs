using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Sonorize.Core.Settings;

namespace Sonorize.Core.Updates;

public class GitHubUpdateService : IDisposable
{
    private const string ReleaseApiUrl = "https://api.github.com/repos/Vraxo/Sonorize/releases/latest";

    private readonly HttpClient _http;
    private readonly ILogger<GitHubUpdateService> _logger;
    private readonly SonorizeSettings _settings;
    private readonly ISettingsManager<SonorizeSettings> _settingsManager;

    public event Action<ReleaseInfo>? UpdateDetected;

    public GitHubUpdateService(
        ILogger<GitHubUpdateService> logger,
        SonorizeSettings settings,
        ISettingsManager<SonorizeSettings> settingsManager)
        : this(logger, settings, settingsManager, new HttpClient())
    {
    }

    internal GitHubUpdateService(
        ILogger<GitHubUpdateService> logger,
        SonorizeSettings settings,
        ISettingsManager<SonorizeSettings> settingsManager,
        HttpClient http)
    {
        _logger = logger;
        _settings = settings;
        _settingsManager = settingsManager;
        _http = http;
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("Sonorize-Updater");
    }

    public async Task CheckForUpdateAsync()
    {
        if (IsRateLimitCooldownActive())
        {
            return;
        }

        try
        {
            using HttpResponseMessage response = await _http.GetAsync(
                ReleaseApiUrl,
                HttpCompletionOption.ResponseHeadersRead);

            if (await HandleRateLimitAsync(response))
            {
                return;
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogDebug("GitHub has no published stable release for Sonorize.");
                return;
            }

            _ = response.EnsureSuccessStatusCode();

            ClearExpiredRateLimitCooldown();

            GitHubRelease? latest = await response.Content.ReadFromJsonAsync<GitHubRelease>();

            if (latest is null)
            {
                return;
            }

            Version? currentVersion = Assembly.GetEntryAssembly()?.GetName().Version;

            if (currentVersion is null)
            {
                return;
            }

            string cleanTag = latest.TagName.TrimStart('v');

            if (!Version.TryParse(cleanTag, out Version? remoteVersion) || remoteVersion <= currentVersion)
            {
                return;
            }

            GitHubAsset? asset = latest.Assets.FirstOrDefault(a =>
                a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));

            if (asset is null)
            {
                return;
            }

            ReleaseInfo release = new()
            {
                Version = latest.TagName,
                ReleaseNotes = latest.Body,
                DownloadUrl = asset.BrowserDownloadUrl,
                SizeBytes = asset.Size
            };

            UpdateDetected?.Invoke(release);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Could not check GitHub for updates.");
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "The GitHub update check timed out.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Update check failed.");
        }
    }

    private bool IsRateLimitCooldownActive()
    {
        DateTimeOffset? retryAt = _settings.Updates.RateLimitResetUtc;

        if (!retryAt.HasValue)
        {
            return false;
        }

        if (retryAt.Value > DateTimeOffset.UtcNow)
        {
            _logger.LogDebug(
                "Skipping GitHub update check until the API rate limit resets at {RetryAtUtc}.",
                retryAt.Value);

            return true;
        }

        _settings.Updates.RateLimitResetUtc = null;
        _settingsManager.Save(_settings);
        return false;
    }

    private async Task<bool> HandleRateLimitAsync(HttpResponseMessage response)
    {
        if (!await IsRateLimitResponseAsync(response))
        {
            return false;
        }

        DateTimeOffset retryAt = GetRateLimitRetryTime(response);
        _settings.Updates.RateLimitResetUtc = retryAt;
        _settingsManager.Save(_settings);

        _logger.LogWarning(
            "GitHub update checks are rate limited. Sonorize will retry after {RetryAtUtc}.",
            retryAt);

        return true;
    }

    private static async Task<bool> IsRateLimitResponseAsync(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            return true;
        }

        if (response.StatusCode != HttpStatusCode.Forbidden)
        {
            return false;
        }

        if (response.Headers.RetryAfter is not null || HasRateLimitRemaining(response, "0"))
        {
            return true;
        }

        string body = await response.Content.ReadAsStringAsync();
        return body.Contains("rate limit", StringComparison.OrdinalIgnoreCase);
    }

    private static DateTimeOffset GetRateLimitRetryTime(HttpResponseMessage response)
    {
        if (response.Headers.RetryAfter?.Date is DateTimeOffset retryDate)
        {
            return retryDate;
        }

        if (response.Headers.RetryAfter?.Delta is TimeSpan retryDelay)
        {
            return DateTimeOffset.UtcNow.Add(retryDelay);
        }

        if (HasRateLimitRemaining(response, "0")
            && response.Headers.TryGetValues("X-RateLimit-Reset", out IEnumerable<string>? resetValues)
            && long.TryParse(
                resetValues.FirstOrDefault(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out long resetUnixSeconds))
        {
            try
            {
                return DateTimeOffset.FromUnixTimeSeconds(resetUnixSeconds);
            }
            catch (ArgumentOutOfRangeException)
            {
                // Fall back to a short delay when the server returns an invalid reset time.
            }
        }

        return DateTimeOffset.UtcNow.AddMinutes(1);
    }

    private static bool HasRateLimitRemaining(HttpResponseMessage response, string expected)
    {
        return response.Headers.TryGetValues("X-RateLimit-Remaining", out IEnumerable<string>? values)
            && string.Equals(values.FirstOrDefault(), expected, StringComparison.Ordinal);
    }

    private void ClearExpiredRateLimitCooldown()
    {
        if (!_settings.Updates.RateLimitResetUtc.HasValue)
        {
            return;
        }

        _settings.Updates.RateLimitResetUtc = null;
        _settingsManager.Save(_settings);
    }

    public void SimulateUpdate(ReleaseInfo release)
    {
        UpdateDetected?.Invoke(release);
    }

    public async Task DownloadUpdateAsync(string url, string destinationPath, IProgress<double> progress)
    {
        bool isLocalFile =
            !url.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            && File.Exists(url);

        if (isLocalFile)
        {
            using FileStream sourceStream = File.OpenRead(url);

            await ProcessDownloadStreamAsync(
                sourceStream,
                destinationPath,
                sourceStream.Length,
                progress,
                isSimulated: true);
        }
        else
        {
            using HttpResponseMessage response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            _ = response.EnsureSuccessStatusCode();
            long? totalBytes = response.Content.Headers.ContentLength;

            using Stream stream = await response.Content.ReadAsStreamAsync();

            await ProcessDownloadStreamAsync(
                stream,
                destinationPath,
                totalBytes,
                progress,
                isSimulated: false);
        }
    }

    private static async Task ProcessDownloadStreamAsync(
        Stream source,
        string destinationPath,
        long? totalBytes,
        IProgress<double> progress,
        bool isSimulated)
    {
        using FileStream destStream = new(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);

        byte[] buffer = new byte[8192];
        long totalRead = 0;
        int bytesRead;

        while ((bytesRead = await source.ReadAsync(buffer)) != 0)
        {
            await destStream.WriteAsync(buffer.AsMemory(0, bytesRead));

            totalRead += bytesRead;

            if (totalBytes.HasValue && totalBytes.Value > 0)
            {
                progress.Report((double)totalRead / totalBytes.Value);
            }

            if (!isSimulated)
            {
                continue;
            }

            // Artificial delay to visualize progress for local files (too fast otherwise)
            await Task.Delay(10);
        }
    }

    public void Dispose()
    {
        _http.Dispose();
        GC.SuppressFinalize(this);
    }
}
