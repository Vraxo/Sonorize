using Sonorize.Core.Models;
using Sonorize.Core.Services.System;
using System.Net.Http.Json;
using System.Reflection;

namespace Sonorize.Core.Services.Update;

public class GitHubUpdateService : IDisposable
{
    private readonly HttpClient _http;
    private readonly LogService _logger;

    public event Action<ReleaseInfo>? UpdateDetected;

    private const string RepoOwner = "Sonorize";
    private const string RepoName = "Sonorize";

    public GitHubUpdateService(LogService logger)
    {
        _logger = logger;
        _http = new();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("Sonorize-Updater");
    }

    public async Task CheckForUpdateAsync()
    {
        try
        {
            var releases = await _http.GetFromJsonAsync<List<GitHubRelease>>($"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases");
            GitHubRelease? latest = releases?.FirstOrDefault(r => !r.Prerelease);

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

            GitHubAsset? asset = latest.Assets.FirstOrDefault(a => a.Name.EndsWith(".zip"));

            if (asset is null)
            {
                return;
            }

            var release = new ReleaseInfo
            {
                Version = latest.TagName,
                ReleaseNotes = latest.Body,
                DownloadUrl = asset.BrowserDownloadUrl,
                SizeBytes = asset.Size
            };

            UpdateDetected?.Invoke(release);
        }
        catch (Exception ex)
        {
            _logger.Warn($"Update check failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Manually injects a release info object to trigger the update flow.
    /// Used for testing or forced updates.
    /// </summary>
    public void SimulateUpdate(ReleaseInfo release)
    {
        UpdateDetected?.Invoke(release);
    }

    public async Task DownloadUpdateAsync(string url, string destinationPath, IProgress<double> progress)
    {
        bool isLocalFile = !url.StartsWith("http", StringComparison.OrdinalIgnoreCase) && File.Exists(url);

        if (isLocalFile)
        {
            using var sourceStream = File.OpenRead(url);
            await ProcessDownloadStreamAsync(sourceStream, destinationPath, sourceStream.Length, progress, isSimulated: true);
        }
        else
        {
            using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            _ = response.EnsureSuccessStatusCode();
            long? totalBytes = response.Content.Headers.ContentLength;

            using var stream = await response.Content.ReadAsStreamAsync();
            await ProcessDownloadStreamAsync(stream, destinationPath, totalBytes, progress, isSimulated: false);
        }
    }

    private static async Task ProcessDownloadStreamAsync(Stream source, string destinationPath, long? totalBytes, IProgress<double> progress, bool isSimulated)
    {
        using var destStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);

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

            if (isSimulated)
            {
                // Artificial delay to visualize progress for local files (too fast otherwise)
                await Task.Delay(10);
            }
        }
    }

    public void Dispose()
    {
        _http.Dispose();
        GC.SuppressFinalize(this);
    }
}