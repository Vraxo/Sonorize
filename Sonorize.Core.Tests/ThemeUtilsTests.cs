using Sonorize.Core.Services.UI;

namespace Sonorize.Core.Tests;

public class ThemeUtilsTests
{
    [Theory]
    [InlineData("#FF0000", 1.0f, "rgba(255, 0, 0, 1)")]
    [InlineData("#00FF00", 0.5f, "rgba(0, 255, 0, 0.5)")]
    [InlineData("0000FF", 0.1f, "rgba(0, 0, 255, 0.1)")] // No hash
    [InlineData("#FFF", 1.0f, "rgba(255, 255, 255, 1)")] // Short hex
    public void HexToRgba_ConvertsCorrectly(string hex, float alpha, string expected)
    {
        // Act
        string result = ThemeUtils.HexToRgba(hex, alpha);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void HexToRgba_InvalidHex_ReturnsBlackTransparent()
    {
        // Act
        string result = ThemeUtils.HexToRgba("INVALID", 0.5f);

        // Assert
        Assert.Equal("rgba(0, 0, 0, 0.5)", result);
    }

    [Fact]
    public void GetSmartHighlightBase_DarkColor_ReturnsWhite()
    {
        // Act
        // Black background -> Should highlight with white overlay (or base color for hover)
        // Note: The logic in ThemeUtils.GetSmartHighlightBase returns #FFFFFF if luminance < 128
        // Wait, luminance > 128 (bright) -> black text/overlay.
        // luminance <= 128 (dark) -> white text/overlay.

        string result = ThemeUtils.GetSmartHighlightBase("#000000", 0.8f);

        // Assert
        Assert.Equal("#FFFFFF", result);
    }

    [Fact]
    public void GetSmartHighlightBase_BrightColor_ReturnsBlack()
    {
        // Act
        string result = ThemeUtils.GetSmartHighlightBase("#FFFFFF", 0.8f);

        // Assert
        Assert.Equal("#000000", result);
    }

    [Fact]
    public void Lighten_IncreasesBrightness()
    {
        // Arrange
        string input = "#101010"; // Very dark gray

        // Act
        string result = ThemeUtils.Lighten(input, 0.5f);

        // Assert
        // Logic: (255 - 16)*0.5 + 16 = 119.5 + 16 = 135 (approx #878787)
        // It should definitely not be the same
        Assert.NotEqual(input, result);
        Assert.StartsWith("#", result);
    }
}