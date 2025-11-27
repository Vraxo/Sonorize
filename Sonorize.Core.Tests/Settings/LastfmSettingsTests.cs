using Sonorize.Core.Settings;

namespace Sonorize.Core.Tests.Settings;

public class LastfmSettingsTests
{
    [Fact]
    public void Defaults_AreCorrectForLastFmStandards()
    {
        // Arrange & Act
        var settings = new LastfmSettings();

        // Assert
        Assert.False(settings.ScrobblingEnabled, "Should be disabled by default.");
        Assert.Equal(50, settings.ScrobbleThresholdPercentage);
        Assert.Equal(240, settings.ScrobbleThresholdAbsoluteSeconds);
    }
}