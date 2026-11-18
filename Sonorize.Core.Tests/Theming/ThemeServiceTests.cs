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
    public void ExportThemePackage_UsesRelativeBackgroundAsset_AndKeepsSavedThemeUnchanged()
    {
        string themesDirectory = Path.Combine(
            Path.GetTempPath(),
            "Sonorize.ThemeService.Tests",
            Guid.NewGuid().ToString("N"));

        try
        {
            ThemeService service = new(themesDirectory, loadBuiltInThemes: false);
            string backgroundPath = Path.Combine(themesDirectory, "cover.png");
            byte[] backgroundBytes = [10, 20, 30, 40, 50];
            File.WriteAllBytes(backgroundPath, backgroundBytes);

            service.SaveTheme("Night", new SonorizeTheme
            {
                AccentColor = "#123456",
                BackgroundImagePath = backgroundPath,
                CustomCss = ".player-bar { opacity: 0.9; }"
            });

            using MemoryStream packageStream = new();
            service.ExportThemePackage(
                "Night",
                packageStream,
                description: "A portable dark theme",
                author: "Sonorize");

            Assert.True(packageStream.CanRead);
            packageStream.Position = 0;

            ThemePackage package = ThemePackageArchive.Read(packageStream);

            Assert.Equal("Night", package.Manifest.Name);
            Assert.Equal("A portable dark theme", package.Manifest.Description);
            Assert.Equal("Sonorize", package.Manifest.Author);
            Assert.Equal("#123456", package.Colors.AccentColor);
            Assert.Equal("assets/backgrounds/background.png", package.Layout.BackgroundImagePath);
            Assert.Equal(".player-bar { opacity: 0.9; }", package.Layout.CustomCss);
            Assert.Equal(backgroundBytes, package.Assets["backgrounds/background.png"]);

            SonorizeTheme savedTheme = Assert.IsType<SonorizeTheme>(service.LoadTheme("Night"));
            Assert.Equal(backgroundPath, savedTheme.BackgroundImagePath);
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
    public void ExportThemePackage_WhenBackgroundIsMissing_ThrowsFileNotFound()
    {
        string themesDirectory = Path.Combine(
            Path.GetTempPath(),
            "Sonorize.ThemeService.Tests",
            Guid.NewGuid().ToString("N"));

        try
        {
            ThemeService service = new(themesDirectory, loadBuiltInThemes: false);
            service.SaveTheme("Missing Background", new SonorizeTheme
            {
                BackgroundImagePath = Path.Combine(themesDirectory, "missing.png")
            });

            using MemoryStream packageStream = new();

            Assert.Throws<FileNotFoundException>(
                () => service.ExportThemePackage("Missing Background", packageStream));
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
    public void ImportThemePackage_ExtractsAssetsAndPreservesPackageStyles()
    {
        string themesDirectory = Path.Combine(
            Path.GetTempPath(),
            "Sonorize.ThemeService.Tests",
            Guid.NewGuid().ToString("N"));

        try
        {
            ThemeService service = new(themesDirectory, loadBuiltInThemes: false);
            byte[] backgroundBytes = [9, 8, 7, 6, 5];
            string stylesCss = ".player-bar { opacity: 0.85; }";
            using MemoryStream packageStream = CreateThemePackageStream(new ThemePackage
            {
                Manifest = new ThemePackageManifest
                {
                    Name = "Portable Night",
                    Description = "Imported package",
                    Author = "Test"
                },
                Colors = new ThemeColors
                {
                    AccentColor = "#123456"
                },
                Layout = new ThemeLayout
                {
                    BorderRadius = 12,
                    BackgroundImagePath = "assets/backgrounds/cover.png",
                    CustomCss = stylesCss
                },
                Assets =
                {
                    ["backgrounds/cover.png"] = backgroundBytes,
                    ["fonts/example.woff2"] = [1, 3, 5]
                }
            });

            string importedName = service.ImportThemePackage(packageStream);

            Assert.Equal("Portable Night", importedName);
            Assert.Contains(importedName, service.GetAvailableColorThemes());

            string assetsDirectory = Path.Combine(themesDirectory, $"{importedName}.assets");
            string importedBackgroundPath = Path.Combine(assetsDirectory, "backgrounds", "cover.png");
            string importedFontPath = Path.Combine(assetsDirectory, "fonts", "example.woff2");

            Assert.True(File.Exists(importedBackgroundPath));
            Assert.True(File.Exists(importedFontPath));
            Assert.Equal(backgroundBytes, File.ReadAllBytes(importedBackgroundPath));

            SonorizeTheme loaded = Assert.IsType<SonorizeTheme>(service.LoadTheme(importedName));
            Assert.Equal("#123456", loaded.AccentColor);
            Assert.Equal(12, loaded.BorderRadius);
            Assert.Equal(stylesCss, loaded.CustomCss);
            Assert.Equal(importedBackgroundPath, loaded.BackgroundImagePath);

            service.DeleteTheme(importedName);

            Assert.False(Directory.Exists(assetsDirectory));
            Assert.Null(service.LoadTheme(importedName));
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
    public void ImportThemePackage_DoesNotOverwriteAnExistingTheme()
    {
        string themesDirectory = Path.Combine(
            Path.GetTempPath(),
            "Sonorize.ThemeService.Tests",
            Guid.NewGuid().ToString("N"));

        try
        {
            ThemeService service = new(themesDirectory, loadBuiltInThemes: false);
            service.SaveTheme("Existing", new SonorizeTheme
            {
                AccentColor = "#112233"
            });

            using MemoryStream packageStream = CreateThemePackageStream(new ThemePackage
            {
                Manifest = new ThemePackageManifest { Name = "existing" },
                Colors = new ThemeColors { AccentColor = "#AABBCC" }
            });

            Assert.Throws<InvalidOperationException>(() => service.ImportThemePackage(packageStream));

            SonorizeTheme existing = Assert.IsType<SonorizeTheme>(service.LoadTheme("Existing"));
            Assert.Equal("#112233", existing.AccentColor);
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
    public void ImportThemePackage_RejectsBackgroundOutsidePackageAssets()
    {
        string themesDirectory = Path.Combine(
            Path.GetTempPath(),
            "Sonorize.ThemeService.Tests",
            Guid.NewGuid().ToString("N"));

        try
        {
            ThemeService service = new(themesDirectory, loadBuiltInThemes: false);
            using MemoryStream packageStream = CreateThemePackageStream(new ThemePackage
            {
                Manifest = new ThemePackageManifest { Name = "Unsafe Background" },
                Layout = new ThemeLayout
                {
                    BackgroundImagePath = Path.Combine(Path.GetTempPath(), "outside.png")
                }
            });

            Assert.Throws<InvalidDataException>(() => service.ImportThemePackage(packageStream));
            Assert.Empty(service.GetAvailableColorThemes());
            Assert.Empty(Directory.EnumerateDirectories(themesDirectory));
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
    public void ImportThemePackage_RejectsMissingBackgroundAsset()
    {
        string themesDirectory = Path.Combine(
            Path.GetTempPath(),
            "Sonorize.ThemeService.Tests",
            Guid.NewGuid().ToString("N"));

        try
        {
            ThemeService service = new(themesDirectory, loadBuiltInThemes: false);
            using MemoryStream packageStream = CreateThemePackageStream(new ThemePackage
            {
                Manifest = new ThemePackageManifest { Name = "Missing Asset" },
                Layout = new ThemeLayout
                {
                    BackgroundImagePath = "assets/backgrounds/missing.png"
                }
            });

            Assert.Throws<InvalidDataException>(() => service.ImportThemePackage(packageStream));
            Assert.Empty(service.GetAvailableColorThemes());
            Assert.Empty(Directory.EnumerateDirectories(themesDirectory));
        }
        finally
        {
            if (Directory.Exists(themesDirectory))
            {
                Directory.Delete(themesDirectory, recursive: true);
            }
        }
    }

    private static MemoryStream CreateThemePackageStream(ThemePackage package)
    {
        MemoryStream stream = new();
        ThemePackageArchive.Write(stream, package);
        stream.Position = 0;
        return stream;
    }

    [Fact]
    public void DeleteTheme_RejectsPathSegments()
    {
        string themesDirectory = Path.Combine(
            Path.GetTempPath(),
            "Sonorize.ThemeService.Tests",
            Guid.NewGuid().ToString("N"));

        try
        {
            ThemeService service = new(themesDirectory, loadBuiltInThemes: false);

            Assert.Throws<ArgumentException>(() => service.DeleteTheme("../outside"));
            Assert.Empty(Directory.EnumerateFiles(themesDirectory));
            Assert.Empty(Directory.EnumerateDirectories(themesDirectory));
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
