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
    public void GenerateRootCss_EmitsSemanticDesignTokens_AndPreservesLegacyVariables()
    {
        SonorizeTheme theme = new()
        {
            AccentColor = "#123456",
            BorderRadius = 6,
            UsePillButtons = false,
            BaseFontSize = 16,
            RowVerticalPadding = 10,
            SidebarItemPadding = 14
        };

        string css = ThemeUtils.GenerateRootCss(theme, useTransparency: false);

        Assert.Contains("--accent-primary: #123456;", css);
        Assert.Contains("--border-radius: 6px;", css);
        Assert.Contains("--btn-radius: 6px;", css);
        Assert.Contains("--base-font-size: 16px;", css);

        Assert.Contains("--color-accent: var(--accent-primary);", css);
        Assert.Contains("--color-background: var(--bg-primary);", css);
        Assert.Contains("--color-surface-raised: var(--bg-tertiary);", css);
        Assert.Contains("--color-text-primary: var(--text-primary);", css);
        Assert.Contains("--color-control-track: #535353;", css);
        Assert.Contains("--shadow-control-thumb: 0 2px 4px rgba(0, 0, 0, 0.3);", css);

        Assert.Contains("--space-1: 4px;", css);
        Assert.Contains("--space-8: 48px;", css);
        Assert.Contains("--space-row-y: var(--row-padding);", css);
        Assert.Contains("--space-sidebar-item: var(--sidebar-item-padding);", css);

        Assert.Contains("--radius-control: var(--border-radius);", css);
        Assert.Contains("--radius-button: var(--btn-radius);", css);
        Assert.Contains("--radius-card: calc(var(--border-radius) * 2);", css);

        Assert.Contains("--font-size-body: var(--base-font-size);", css);
        Assert.Contains("--font-size-heading: calc(var(--base-font-size) * 1.25);", css);
        Assert.Contains("--line-height-normal: 1.5;", css);

        Assert.Contains("--motion-duration-fast: 120ms;", css);
        Assert.Contains("--motion-easing-standard: cubic-bezier(0.2, 0, 0, 1);", css);
        Assert.Contains("--focus-ring:", css);
        Assert.Contains("--shadow-raised:", css);
    }

    [Fact]
    public void GenerateRootCss_SemanticColorTokensFollowTransparentLegacyColors()
    {
        SonorizeTheme theme = new()
        {
            BgPrimary = "#112233",
            SidebarOpacity = 0.4f
        };

        string css = ThemeUtils.GenerateRootCss(theme, useTransparency: true);

        Assert.Contains("--bg-primary: rgba(17, 34, 51, 0.4);", css);
        Assert.Contains("--color-background: var(--bg-primary);", css);
    }
}
