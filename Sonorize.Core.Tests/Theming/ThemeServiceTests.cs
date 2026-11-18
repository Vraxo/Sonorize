using Sonorize.Core.Theming;

namespace Sonorize.Core.Tests.Theming;

public class ThemeServiceTests
{
    [Fact]
    public void SaveTheme_AndLoadTheme_RoundTripsColorsAndLayout()
    {
        string themesDirectory = Path.Combine(
            Path.GetTempPath(),
            "Sonorize.ThemeService.Tests",
            Guid.NewGuid().ToString("N"));

        try
        {
            ThemeService service = new(themesDirectory, loadBuiltInThemes: false);
            SonorizeTheme theme = new()
            {
                AccentColor = "#123456",
                BgPrimary = "#101010",
                BgSecondary = "#202020",
                BgTertiary = "#303030",
                PlayerBarBg = "#181818",
                BorderColor = "#404040",
                TextPrimary = "#F0F0F0",
                TextSecondary = "#A0A0A0",
                BorderRadius = 9,
                UsePillButtons = false,
                EnableAmbientBackground = true,
                PlayerBarHeight = 104,
                SidebarOpacity = 0.72f,
                MainContentOpacity = 0.81f,
                PlayerBarOpacity = 0.93f,
                HighlightOpacity = 0.2f,
                BackgroundImagePath = "backgrounds/cover.png",
                BackgroundBlur = 18,
                BackgroundBrightness = 0.65f,
                ContentGradientHeight = 280,
                CustomFontFamily = "Example Sans",
                BaseFontSize = 16,
                CustomCss = ".player-bar { opacity: 0.9; }",
                RowVerticalPadding = 14,
                SidebarItemPadding = 12,
                EnableZebraStriping = true,
                ShowGridLines = false
            };

            service.SaveTheme("Night", theme);

            SonorizeTheme loaded = Assert.IsType<SonorizeTheme>(service.LoadTheme("Night"));

            Assert.Equal(theme.AccentColor, loaded.AccentColor);
            Assert.Equal(theme.BgPrimary, loaded.BgPrimary);
            Assert.Equal(theme.BgSecondary, loaded.BgSecondary);
            Assert.Equal(theme.BgTertiary, loaded.BgTertiary);
            Assert.Equal(theme.PlayerBarBg, loaded.PlayerBarBg);
            Assert.Equal(theme.BorderColor, loaded.BorderColor);
            Assert.Equal(theme.TextPrimary, loaded.TextPrimary);
            Assert.Equal(theme.TextSecondary, loaded.TextSecondary);

            Assert.Equal(theme.BorderRadius, loaded.BorderRadius);
            Assert.Equal(theme.UsePillButtons, loaded.UsePillButtons);
            Assert.Equal(theme.EnableAmbientBackground, loaded.EnableAmbientBackground);
            Assert.Equal(theme.PlayerBarHeight, loaded.PlayerBarHeight);
            Assert.Equal(theme.SidebarOpacity, loaded.SidebarOpacity);
            Assert.Equal(theme.MainContentOpacity, loaded.MainContentOpacity);
            Assert.Equal(theme.PlayerBarOpacity, loaded.PlayerBarOpacity);
            Assert.Equal(theme.HighlightOpacity, loaded.HighlightOpacity);
            Assert.Equal(theme.BackgroundImagePath, loaded.BackgroundImagePath);
            Assert.Equal(theme.BackgroundBlur, loaded.BackgroundBlur);
            Assert.Equal(theme.BackgroundBrightness, loaded.BackgroundBrightness);
            Assert.Equal(theme.ContentGradientHeight, loaded.ContentGradientHeight);
            Assert.Equal(theme.CustomFontFamily, loaded.CustomFontFamily);
            Assert.Equal(theme.BaseFontSize, loaded.BaseFontSize);
            Assert.Equal(theme.CustomCss, loaded.CustomCss);
            Assert.Equal(theme.RowVerticalPadding, loaded.RowVerticalPadding);
            Assert.Equal(theme.SidebarItemPadding, loaded.SidebarItemPadding);
            Assert.Equal(theme.EnableZebraStriping, loaded.EnableZebraStriping);
            Assert.Equal(theme.ShowGridLines, loaded.ShowGridLines);

            Assert.True(File.Exists(Path.Combine(themesDirectory, "Night.colors.json")));
            Assert.True(File.Exists(Path.Combine(themesDirectory, "Night.layout.json")));
            Assert.Equal(["Night"], service.GetAvailableColorThemes());
            Assert.Equal(["Default", "Night"], service.GetAvailableLayoutThemes());
        }
        finally
        {
            if (Directory.Exists(themesDirectory))
            {
                Directory.Delete(themesDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public void LoadTheme_WhenLayoutPresetIsMissing_UsesDefaultLayout()
    {
        string themesDirectory = Path.Combine(
            Path.GetTempPath(),
            "Sonorize.ThemeService.Tests",
            Guid.NewGuid().ToString("N"));

        try
        {
            ThemeService service = new(themesDirectory, loadBuiltInThemes: false);
            service.SaveThemeColors("Colors Only", new ThemeColors
            {
                AccentColor = "#654321"
            });

            SonorizeTheme loaded = Assert.IsType<SonorizeTheme>(service.LoadTheme("Colors Only"));

            Assert.Equal("#654321", loaded.AccentColor);
            Assert.Equal(new ThemeLayout().BorderRadius, loaded.BorderRadius);
        }
        finally
        {
            if (Directory.Exists(themesDirectory))
            {
                Directory.Delete(themesDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public void DeleteTheme_RemovesColorAndLayoutFiles()
    {
        string themesDirectory = Path.Combine(
            Path.GetTempPath(),
            "Sonorize.ThemeService.Tests",
            Guid.NewGuid().ToString("N"));

        try
        {
            ThemeService service = new(themesDirectory, loadBuiltInThemes: false);
            service.SaveTheme("Delete Me", new SonorizeTheme());

            service.DeleteTheme("Delete Me");

            Assert.Empty(service.GetAvailableColorThemes());
            Assert.Equal(["Default"], service.GetAvailableLayoutThemes());
        }
        finally
        {
            if (Directory.Exists(themesDirectory))
            {
                Directory.Delete(themesDirectory, recursive: true);
            }
        }
    }
}
