using Sonorize.Core.Theming;

namespace Sonorize.Core.Tests.Theming;

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

    [Theory]
    [InlineData("#D9BBFB", "#000000")]
    [InlineData("#123456", "#FFFFFF")]
    [InlineData("#777777", "#000000")]
    [InlineData("invalid", "#000000")]
    public void GetContrastingColor_ReturnsTheHigherContrastForeground(string color, string expected)
    {
        Assert.Equal(expected, ThemeUtils.GetContrastingColor(color));
    }

    [Fact]
    public void GetSmartHighlightBase_DarkColor_ReturnsWhite()
    {
        // Black background -> white highlight for a dark color.
        string result = ThemeUtils.GetSmartHighlightBase("#000000", 0.8f);

        Assert.Equal("#FFFFFF", result);
    }

    [Fact]
    public void GetSmartHighlightBase_BrightColor_ReturnsBlack()
    {
        string result = ThemeUtils.GetSmartHighlightBase("#FFFFFF", 0.8f);

        Assert.Equal("#000000", result);
    }

    [Fact]
    public void Lighten_IncreasesBrightness()
    {
        string input = "#101010";

        string result = ThemeUtils.Lighten(input, 0.5f);

        Assert.NotEqual(input, result);
        Assert.StartsWith("#", result);
    }

    [Fact]
    public void GenerateRootCss_EmitsRuntimeThemeValues_WithoutDuplicatingSharedTokens()
    {
        SonorizeTheme theme = new()
        {
            AccentColor = "#123456",
            DangerColor = "#C1121F",
            WarningColor = "#F0A500",
            BgPrimary = "#101010",
            BorderRadius = 6,
            UsePillButtons = false,
            BaseFontSize = 16,
            RowVerticalPadding = 10,
            SidebarItemPadding = 14,
            PlayerBarHeight = 96
        };

        string css = ThemeUtils.GenerateRootCss(theme, useTransparency: false);

        Assert.Contains("--accent-primary: #123456;", css);
        Assert.Contains("--color-accent-contrast: #FFFFFF;", css);
        Assert.Contains("--color-accent-hover-contrast: #FFFFFF;", css);
        Assert.Contains("--color-danger: #C1121F;", css);
        Assert.Contains("--color-warning: #F0A500;", css);
        Assert.Contains("--bg-primary: #101010;", css);
        Assert.Contains("--border-radius: 6px;", css);
        Assert.Contains("--btn-radius: 6px;", css);
        Assert.Contains("--base-font-size: 16px;", css);
        Assert.Contains("--row-padding: 10px;", css);
        Assert.Contains("--sidebar-item-padding: 14px;", css);
        Assert.Contains("--player-bar-height: 96px;", css);

        Assert.DoesNotContain("--color-accent:", css);
        Assert.DoesNotContain("--space-1:", css);
        Assert.DoesNotContain("--radius-button:", css);
        Assert.DoesNotContain("--font-size-body:", css);
        Assert.DoesNotContain("--motion-duration-standard:", css);
    }

    [Fact]
    public void GenerateRootCss_EmitsTransparentRuntimeColors()
    {
        SonorizeTheme theme = new()
        {
            BgPrimary = "#112233",
            SidebarOpacity = 0.4f
        };

        string css = ThemeUtils.GenerateRootCss(theme, useTransparency: true);

        Assert.Contains("--bg-primary: rgba(17, 34, 51, 0.4);", css);
        Assert.DoesNotContain("--color-background:", css);
    }
}
