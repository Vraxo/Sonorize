using Sonorize.Core.Models;
using Sonorize.Core.Services.Scrobbling;
using Sonorize.Core.Settings;

namespace Sonorize.Core.Tests.Services;

public class ScrobbleEligibilityServiceTests
{
    private readonly ScrobbleEligibilityService _service;
    private readonly LastfmSettings _settings;

    public ScrobbleEligibilityServiceTests()
    {
        _service = new ScrobbleEligibilityService();
        _settings = new LastfmSettings
        {
            ScrobblingEnabled = true,
            ScrobbleThresholdPercentage = 50,
            ScrobbleThresholdAbsoluteSeconds = 240 // 4 minutes
        };
    }

    [Fact]
    public void ShouldScrobble_SongTooShort_ReturnsFalse()
    {
        // Arrange
        var song = new Song { FilePath = "short.mp3", Duration = TimeSpan.FromSeconds(29) };
        var played = TimeSpan.FromSeconds(29);

        // Act
        bool result = _service.ShouldScrobble(song, played, _settings);

        // Assert
        Assert.False(result, "Songs under 30 seconds should never scrobble (Last.fm rule).");
    }

    [Fact]
    public void ShouldScrobble_PercentageMet_ReturnsTrue()
    {
        // Arrange
        // Song: 200s (3m 20s). 50% = 100s.
        var song = new Song { FilePath = "song.mp3", Duration = TimeSpan.FromSeconds(200) };
        var played = TimeSpan.FromSeconds(101); // Just over 50%

        // Act
        bool result = _service.ShouldScrobble(song, played, _settings);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldScrobble_PercentageNotMet_ReturnsFalse()
    {
        // Arrange
        // Song: 200s. 50% = 100s.
        var song = new Song { FilePath = "song.mp3", Duration = TimeSpan.FromSeconds(200) };
        var played = TimeSpan.FromSeconds(90);

        // Act
        bool result = _service.ShouldScrobble(song, played, _settings);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldScrobble_LongSong_AbsoluteThresholdMet_ReturnsTrue()
    {
        // Arrange
        // Song: 600s (10m). 50% = 300s (5m). Absolute Limit = 240s (4m).
        // Rule: Whichever is shorter. So required is 240s.
        var song = new Song { FilePath = "long.mp3", Duration = TimeSpan.FromMinutes(10) };

        // Played: 250s (4m 10s). This is < 50% (300s), but > Absolute (240s).
        var played = TimeSpan.FromSeconds(250);

        // Act
        bool result = _service.ShouldScrobble(song, played, _settings);

        // Assert
        Assert.True(result, "Should scrobble because 4-minute absolute cap was met, even if 50% wasn't.");
    }

    [Fact]
    public void ShouldScrobble_LongSong_AbsoluteThresholdNotMet_ReturnsFalse()
    {
        // Arrange
        // Song: 600s (10m). Effective threshold is 240s.
        var song = new Song { FilePath = "long.mp3", Duration = TimeSpan.FromMinutes(10) };
        var played = TimeSpan.FromSeconds(200); // Less than 240s

        // Act
        bool result = _service.ShouldScrobble(song, played, _settings);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldScrobble_CustomPercentage_Respected()
    {
        // Arrange
        _settings.ScrobbleThresholdPercentage = 90;
        var song = new Song { FilePath = "song.mp3", Duration = TimeSpan.FromSeconds(100) };
        var played = TimeSpan.FromSeconds(85); // 85%, not enough for 90%

        // Act
        bool result = _service.ShouldScrobble(song, played, _settings);

        // Assert
        Assert.False(result);
    }
}