using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Sonorize.Core.Settings;
using Sonorize.Core.Updates;

namespace Sonorize.Core.Tests.Updates;

public class GitHubUpdateServiceTests
{
    [Fact]
    public async Task CheckForUpdateAsync_UsesCorrectRepositoryAndRaisesUpdateEvent()
    {
        using StubHttpMessageHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {
                    "tag_name": "v99.0.0",
                    "body": "Release notes",
                    "prerelease": false,
                    "assets": [
                        {
                            "name": "Sonorize.zip",
                            "browser_download_url": "https://example.com/Sonorize.zip",
                            "size": 123
                        }
                    ]
                }
                """,
                Encoding.UTF8,
                "application/json")
        });
        using HttpClient http = new(handler);

        SonorizeSettings settings = new();
        ISettingsManager<SonorizeSettings> settingsManager = Substitute.For<ISettingsManager<SonorizeSettings>>();
        ILogger<GitHubUpdateService> logger = Substitute.For<ILogger<GitHubUpdateService>>();
        using GitHubUpdateService service = new(logger, settings, settingsManager, http);

        ReleaseInfo? detectedRelease = null;
        service.UpdateDetected += release => detectedRelease = release;

        await service.CheckForUpdateAsync();

        Assert.Equal(
            "https://api.github.com/repos/Vraxo/Sonorize/releases/latest",
            handler.RequestedUris.Single()?.ToString());
        Assert.NotNull(detectedRelease);
        Assert.Equal("v99.0.0", detectedRelease.Version);
        Assert.Equal("Release notes", detectedRelease.ReleaseNotes);
        Assert.Equal(123, detectedRelease.SizeBytes);
    }

    [Fact]
    public async Task CheckForUpdateAsync_PersistsRateLimitResetAndSkipsRequestsUntilThen()
    {
        DateTimeOffset resetAt = DateTimeOffset.UtcNow.AddMinutes(20);
        using StubHttpMessageHandler handler = new(_ =>
        {
            HttpResponseMessage response = new(HttpStatusCode.Forbidden)
            {
                Content = new StringContent("{\"message\":\"API rate limit exceeded\"}")
            };
            response.Headers.TryAddWithoutValidation("X-RateLimit-Remaining", "0");
            response.Headers.TryAddWithoutValidation(
                "X-RateLimit-Reset",
                resetAt.ToUnixTimeSeconds().ToString());

            return response;
        });
        using HttpClient http = new(handler);

        SonorizeSettings settings = new();
        ISettingsManager<SonorizeSettings> settingsManager = Substitute.For<ISettingsManager<SonorizeSettings>>();
        ILogger<GitHubUpdateService> logger = Substitute.For<ILogger<GitHubUpdateService>>();
        using GitHubUpdateService service = new(logger, settings, settingsManager, http);

        await service.CheckForUpdateAsync();
        await service.CheckForUpdateAsync();

        Assert.Equal(1, handler.RequestedUris.Count);
        Assert.Equal(
            DateTimeOffset.FromUnixTimeSeconds(resetAt.ToUnixTimeSeconds()),
            settings.Updates.RateLimitResetUtc);
        settingsManager.Received(1).Save(settings);
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        public List<Uri?> RequestedUris { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestedUris.Add(request.RequestUri);
            return Task.FromResult(responseFactory(request));
        }
    }
}
