using Sonorize.Core.Models;
using Sonorize.Core.Services.System;
using System.IO.Compression;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Sonorize.Core.Services.Update;

public class GitHubUpdateService
{
    private readonly HttpClient _http;
    private readonly LogService _logger;

    public event Action<ReleaseInfo>? UpdateDetected;

    // Configuration
    private const string RepoOwner = "Sonorize";
    private const string RepoName = "Sonorize";

    public GitHubUpdateService(LogService logger)
    {
        _logger = logger;
        _http = new HttpClient();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("Sonorize-Updater");
    }

    public async Task CheckForUpdateAsync()
    {
        try
        {
            var releases = await _http.GetFromJsonAsync<List<GitHubRelease>>($"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases");

            var latest = releases?.FirstOrDefault(r => !r.Prerelease);
            if (latest is null)
            {
                return;
            }

            var currentVersion = Assembly.GetEntryAssembly()?.GetName().Version;
            if (currentVersion is null)
            {
                return;
            }

            string cleanTag = latest.TagName.TrimStart('v');
            if (Version.TryParse(cleanTag, out Version? remoteVersion) && remoteVersion > currentVersion)
            {
                var asset = latest.Assets.FirstOrDefault(a => a.Name.EndsWith(".zip"));

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
        }
        catch (Exception ex)
        {
            // Silent fail on check is expected
            _logger.Warn($"Update check failed: {ex.Message}");
        }
    }

    public void TriggerMockUpdate()
    {
        try
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "SonorizeMock_" + Guid.NewGuid());
            _ = Directory.CreateDirectory(tempDir);

            // Create a dummy file that proves the update worked
            File.WriteAllText(Path.Combine(tempDir, "UPDATE_SUCCESSFUL.txt"), $"Update test completed at {DateTime.Now}");

            string zipPath = Path.Combine(Path.GetTempPath(), "mock-release.zip");
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }

            ZipFile.CreateFromDirectory(tempDir, zipPath);
            Directory.Delete(tempDir, true);

            var mockRelease = new ReleaseInfo
            {
                Version = "v99.9.9-TEST",
                ReleaseNotes = "# Test Update\nThis is a simulated update to test the installation pipeline.\n\n- Verifies ZIP extraction\n- Verifies Script execution\n- Verifies App Restart",
                DownloadUrl = zipPath, // Local path
                SizeBytes = new FileInfo(zipPath).Length,
                IsMandatory = false
            };

            UpdateDetected?.Invoke(mockRelease);
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to trigger mock update", ex);
        }
    }

    public async Task DownloadUpdateAsync(string url, string destinationPath, IProgress<double> progress)
    {
        // Handle Local Mock Files
        if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase) && File.Exists(url))
        {
            using var sourceStream = File.OpenRead(url);
            using var destStream = File.Create(destinationPath);

            long total = sourceStream.Length;
            byte[] buffer = new byte[8192];
            int read;
            long totalRead = 0;

            while ((read = await sourceStream.ReadAsync(buffer)) > 0)
            {
                await destStream.WriteAsync(buffer.AsMemory(0, read));
                totalRead += read;
                progress.Report((double)totalRead / total);
                await Task.Delay(10); // Artificial delay to visualize progress
            }
            return;
        }

        // Handle Real HTTP Downloads
        using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        _ = response.EnsureSuccessStatusCode();

        long? totalBytes = response.Content.Headers.ContentLength;

        using var stream = await response.Content.ReadAsStreamAsync();
        using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);

        var httpBuffer = new byte[8192];
        long httpTotalRead = 0;
        int bytesRead;

        while ((bytesRead = await stream.ReadAsync(httpBuffer)) != 0)
        {
            await fileStream.WriteAsync(httpBuffer.AsMemory(0, bytesRead));
            httpTotalRead += bytesRead;

            if (totalBytes.HasValue)
            {
                progress.Report((double)httpTotalRead / totalBytes.Value);
            }
        }
    }

    private class GitHubRelease
    {
        [JsonPropertyName("tag_name")] public string TagName { get; set; } = "";
        [JsonPropertyName("body")] public string Body { get; set; } = "";
        [JsonPropertyName("prerelease")] public bool Prerelease { get; set; }
        [JsonPropertyName("assets")] public List<GitHubAsset> Assets { get; set; } = [];
    }

    private class GitHubAsset
    {
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("browser_download_url")] public string BrowserDownloadUrl { get; set; } = "";
        [JsonPropertyName("size")] public long Size { get; set; }
    }
}