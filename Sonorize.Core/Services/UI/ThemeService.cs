using Sonorize.Core.Helpers;
using Sonorize.Core.Settings;
using System.Text.Json;

namespace Sonorize.Core.Services.UI;

public class ThemeService
{
    private readonly string _themesDir;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public ThemeService()
    {
        _themesDir = AppDataHelper.GetSubDirectory("Themes");
        EnsureBuiltInThemes();
    }

    private void EnsureBuiltInThemes()
    {
        try
        {
            string builtInThemesDir = Path.Combine(AppContext.BaseDirectory, "Themes");
            if (!Directory.Exists(builtInThemesDir))
                return;

            // Copy all .colors.json and .layout.json files
            foreach (string file in Directory.EnumerateFiles(builtInThemesDir, "*.colors.json"))
            {
                string fileName = Path.GetFileName(file);
                string dest = Path.Combine(_themesDir, fileName);
                if (!File.Exists(dest))
                    File.Copy(file, dest);
            }

            foreach (string file in Directory.EnumerateFiles(builtInThemesDir, "*.layout.json"))
            {
                string fileName = Path.GetFileName(file);
                string dest = Path.Combine(_themesDir, fileName);
                if (!File.Exists(dest))
                    File.Copy(file, dest);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ThemeService] Failed to ensure built-in themes: {ex.Message}");
        }
    }

    // Color themes (from .colors.json files)
    public List<string> GetAvailableColorThemes()
    {
        if (!Directory.Exists(_themesDir))
            return new List<string>();

        return Directory.EnumerateFiles(_themesDir, "*.colors.json")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => name != null && name.EndsWith(".colors"))
            .Select(name => name!.Replace(".colors", ""))
            .OrderBy(x => x)
            .ToList();
    }

    // Layout themes (from .layout.json files)
    public List<string> GetAvailableLayoutThemes()
    {
        var layouts = new List<string> { "Default" };

        if (!Directory.Exists(_themesDir))
            return layouts;

        var customLayouts = Directory.EnumerateFiles(_themesDir, "*.layout.json")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => name != null && name.EndsWith(".layout"))
            .Select(name => name!.Replace(".layout", ""))
            .OrderBy(x => x)
            .ToList();

        layouts.AddRange(customLayouts);
        return layouts;
    }

    public ThemeColors? LoadThemeColors(string name)
    {
        string path = Path.Combine(_themesDir, $"{name}.colors.json");
        if (!File.Exists(path))
            return null;

        try
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<ThemeColors>(json, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public ThemeLayout? LoadThemeLayout(string name)
    {
        string path = Path.Combine(_themesDir, $"{name}.layout.json");
        if (!File.Exists(path))
        {
            // Fallback to default layout
            path = Path.Combine(_themesDir, "default.layout.json");
            if (!File.Exists(path))
                return new ThemeLayout(); // return default instance
        }

        try
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<ThemeLayout>(json, _jsonOptions);
        }
        catch
        {
            return new ThemeLayout();
        }
    }

    public void SaveThemeColors(string name, ThemeColors colors)
    {
        string safeName = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
        string path = Path.Combine(_themesDir, $"{safeName}.colors.json");
        string json = JsonSerializer.Serialize(colors, _jsonOptions);
        File.WriteAllText(path, json);
    }

    public void SaveThemeLayout(string name, ThemeLayout layout)
    {
        string safeName = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
        string path = Path.Combine(_themesDir, $"{safeName}.layout.json");
        string json = JsonSerializer.Serialize(layout, _jsonOptions);
        File.WriteAllText(path, json);
    }

    // Legacy methods for backward compatibility with existing UI
    public List<string> GetAvailableThemes()
    {
        return GetAvailableColorThemes();
    }

    public SonorizeTheme? LoadTheme(string name)
    {
        var colors = LoadThemeColors(name);
        if (colors == null)
            return null;

        var layout = LoadThemeLayout(name);
        return CombineToSonorizeTheme(colors, layout);
    }

    public void SaveTheme(string name, SonorizeTheme theme)
    {
        // Split into colors and layout
        var colors = new ThemeColors
        {
            AccentColor = theme.AccentColor,
            BgPrimary = theme.BgPrimary,
            BgSecondary = theme.BgSecondary,
            BgTertiary = theme.BgTertiary,
            PlayerBarBg = theme.PlayerBarBg,
            BorderColor = theme.BorderColor,
            TextPrimary = theme.TextPrimary,
            TextSecondary = theme.TextSecondary
        };

        var layout = new ThemeLayout
        {
            BorderRadius = theme.BorderRadius,
            UsePillButtons = theme.UsePillButtons,
            EnableAmbientBackground = theme.EnableAmbientBackground,
            EnableCustomScrollbars = theme.EnableCustomScrollbars,
            PlayerBarPosition = theme.PlayerBarPosition,
            SidebarPosition = theme.SidebarPosition,
            PlayerBarHeight = theme.PlayerBarHeight,
            PlayerBarLayout = theme.PlayerBarLayout,
            SidebarOpacity = theme.SidebarOpacity,
            MainContentOpacity = theme.MainContentOpacity,
            PlayerBarOpacity = theme.PlayerBarOpacity,
            HighlightOpacity = theme.HighlightOpacity,
            BackgroundImagePath = theme.BackgroundImagePath,
            BackgroundBlur = theme.BackgroundBlur,
            BackgroundBrightness = theme.BackgroundBrightness,
            ContentGradientHeight = theme.ContentGradientHeight,
            CustomFontFamily = theme.CustomFontFamily,
            BaseFontSize = theme.BaseFontSize,
            CustomCss = theme.CustomCss,
            RowVerticalPadding = theme.RowVerticalPadding,
            SidebarItemPadding = theme.SidebarItemPadding,
            EnableZebraStriping = theme.EnableZebraStriping,
            ShowGridLines = theme.ShowGridLines
        };

        SaveThemeColors(name, colors);
        SaveThemeLayout(name, layout);
    }

    public void DeleteTheme(string name)
    {
        string colorsPath = Path.Combine(_themesDir, $"{name}.colors.json");
        string layoutPath = Path.Combine(_themesDir, $"{name}.layout.json");

        if (File.Exists(colorsPath))
            File.Delete(colorsPath);
        if (File.Exists(layoutPath))
            File.Delete(layoutPath);
    }

    private SonorizeTheme CombineToSonorizeTheme(ThemeColors colors, ThemeLayout layout)
    {
        return new SonorizeTheme
        {
            AccentColor = colors.AccentColor,
            BgPrimary = colors.BgPrimary,
            BgSecondary = colors.BgSecondary,
            BgTertiary = colors.BgTertiary,
            PlayerBarBg = colors.PlayerBarBg,
            BorderColor = colors.BorderColor,
            TextPrimary = colors.TextPrimary,
            TextSecondary = colors.TextSecondary,

            BorderRadius = layout.BorderRadius,
            UsePillButtons = layout.UsePillButtons,
            EnableAmbientBackground = layout.EnableAmbientBackground,
            EnableCustomScrollbars = layout.EnableCustomScrollbars,
            PlayerBarPosition = layout.PlayerBarPosition,
            SidebarPosition = layout.SidebarPosition,
            PlayerBarHeight = layout.PlayerBarHeight,
            PlayerBarLayout = layout.PlayerBarLayout,
            SidebarOpacity = layout.SidebarOpacity,
            MainContentOpacity = layout.MainContentOpacity,
            PlayerBarOpacity = layout.PlayerBarOpacity,
            HighlightOpacity = layout.HighlightOpacity,
            BackgroundImagePath = layout.BackgroundImagePath,
            BackgroundBlur = layout.BackgroundBlur,
            BackgroundBrightness = layout.BackgroundBrightness,
            ContentGradientHeight = layout.ContentGradientHeight,
            CustomFontFamily = layout.CustomFontFamily,
            BaseFontSize = layout.BaseFontSize,
            CustomCss = layout.CustomCss,
            RowVerticalPadding = layout.RowVerticalPadding,
            SidebarItemPadding = layout.SidebarItemPadding,
            EnableZebraStriping = layout.EnableZebraStriping,
            ShowGridLines = layout.ShowGridLines
        };
    }
}