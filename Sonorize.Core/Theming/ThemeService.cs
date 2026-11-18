using System.Text;
using System.Text.Json;
using Sonorize.Core.Core;
using Sonorize.Core.Playback;
using Sonorize.Core.Settings;

namespace Sonorize.Core.Theming;

public class ThemeService
{
    private readonly string _themesDir;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public ThemeService()
        : this(AppDataHelper.GetSubDirectory("Themes"), loadBuiltInThemes: true)
    {
    }

    internal ThemeService(string themesDirectory, bool loadBuiltInThemes)
    {
        _themesDir = Path.GetFullPath(themesDirectory);
        _ = Directory.CreateDirectory(_themesDir);

        if (loadBuiltInThemes)
        {
            EnsureBuiltInThemes();
        }
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

    public void ExportThemePackage(
        string name,
        Stream destination,
        string description = "",
        string author = "",
        string themeVersion = "1.0.0")
    {
        if (!GetAvailableColorThemes().Contains(name, StringComparer.Ordinal))
        {
            throw new KeyNotFoundException($"Theme not found: {name}");
        }

        ThemeColors colors = LoadThemeColors(name)
            ?? throw new InvalidDataException($"Theme colors could not be loaded: {name}");
        ThemeLayout layout = LoadThemeLayout(name) ?? new ThemeLayout();

        ThemePackage package = new()
        {
            Manifest = new ThemePackageManifest
            {
                Name = name,
                Description = description,
                Author = author,
                ThemeVersion = themeVersion
            },
            Colors = colors,
            Layout = layout
        };

        if (!string.IsNullOrWhiteSpace(layout.BackgroundImagePath))
        {
            string backgroundPath = layout.BackgroundImagePath;
            if (!File.Exists(backgroundPath))
            {
                throw new FileNotFoundException("The theme background image could not be found.", backgroundPath);
            }

            string extension = Path.GetExtension(backgroundPath).ToLowerInvariant();
            string assetPath = $"backgrounds/background{extension}";
            package.Assets[assetPath] = ReadThemeAsset(backgroundPath);
            package.Layout.BackgroundImagePath = $"assets/{assetPath}";
        }

        ThemePackageArchive.Write(destination, package);
    }

    public string ImportThemePackage(Stream source)
    {
        ThemePackage package = ThemePackageArchive.Read(source);
        string themeName = NormalizeThemePackageName(package.Manifest.Name);

        if (ThemeExists(themeName))
        {
            throw new InvalidOperationException($"A theme named '{themeName}' already exists.");
        }

        string? backgroundAssetPath = null;
        if (!string.IsNullOrWhiteSpace(package.Layout.BackgroundImagePath))
        {
            const string assetPrefix = "assets/";
            if (!package.Layout.BackgroundImagePath.StartsWith(assetPrefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("Theme package backgrounds must reference an asset inside the package.");
            }

            backgroundAssetPath = package.Layout.BackgroundImagePath[assetPrefix.Length..];
            if (!package.Assets.ContainsKey(backgroundAssetPath))
            {
                throw new InvalidDataException("The theme package background asset is missing.");
            }
        }

        string finalAssetsDirectory = Path.Combine(_themesDir, $"{themeName}.assets");
        string stagingDirectory = Path.Combine(_themesDir, $".{themeName}.{Guid.NewGuid():N}.importing");
        bool assetsMoved = false;
        bool colorsFileCreated = false;
        bool layoutFileCreated = false;
        string colorsPath = Path.Combine(_themesDir, $"{themeName}.colors.json");
        string layoutPath = Path.Combine(_themesDir, $"{themeName}.layout.json");

        try
        {
            if (package.Assets.Count > 0)
            {
                _ = Directory.CreateDirectory(stagingDirectory);

                foreach ((string assetPath, byte[] bytes) in package.Assets)
                {
                    string destinationPath = GetContainedAssetPath(stagingDirectory, assetPath);
                    string? destinationDirectory = Path.GetDirectoryName(destinationPath);
                    if (destinationDirectory is not null)
                    {
                        _ = Directory.CreateDirectory(destinationDirectory);
                    }

                    using FileStream destination = new(
                        destinationPath,
                        FileMode.CreateNew,
                        FileAccess.Write,
                        FileShare.None);
                    destination.Write(bytes, 0, bytes.Length);
                }

                Directory.Move(stagingDirectory, finalAssetsDirectory);
                assetsMoved = true;
            }

            ThemeLayout layout = package.Layout.Clone();
            layout.BackgroundImagePath = backgroundAssetPath is null
                ? null
                : GetContainedAssetPath(finalAssetsDirectory, backgroundAssetPath);

            using (FileStream colorsFile = new(
                colorsPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None))
            {
                colorsFileCreated = true;
                JsonSerializer.Serialize(colorsFile, package.Colors, _jsonOptions);
            }

            using (FileStream layoutFile = new(
                layoutPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None))
            {
                layoutFileCreated = true;
                JsonSerializer.Serialize(layoutFile, layout, _jsonOptions);
            }

            return themeName;
        }
        catch
        {
            if (colorsFileCreated && File.Exists(colorsPath))
            {
                File.Delete(colorsPath);
            }

            if (layoutFileCreated && File.Exists(layoutPath))
            {
                File.Delete(layoutPath);
            }

            if (assetsMoved && Directory.Exists(finalAssetsDirectory))
            {
                Directory.Delete(finalAssetsDirectory, recursive: true);
            }

            throw;
        }
        finally
        {
            if (Directory.Exists(stagingDirectory))
            {
                Directory.Delete(stagingDirectory, recursive: true);
            }
        }
    }

    public void DeleteTheme(string name)
    {
        string colorsPath = Path.Combine(_themesDir, $"{name}.colors.json");
        string layoutPath = Path.Combine(_themesDir, $"{name}.layout.json");

        if (File.Exists(colorsPath))
            File.Delete(colorsPath);
        if (File.Exists(layoutPath))
            File.Delete(layoutPath);

        string assetsDirectory = Path.Combine(_themesDir, $"{name}.assets");
        if (Directory.Exists(assetsDirectory))
            Directory.Delete(assetsDirectory, recursive: true);
    }

    private bool ThemeExists(string name)
    {
        string colorsFileName = $"{name}.colors.json";
        string layoutFileName = $"{name}.layout.json";
        string assetsDirectoryName = $"{name}.assets";

        return Directory.EnumerateFiles(_themesDir)
                .Any(path =>
                    Path.GetFileName(path).Equals(colorsFileName, StringComparison.OrdinalIgnoreCase) ||
                    Path.GetFileName(path).Equals(layoutFileName, StringComparison.OrdinalIgnoreCase))
            || Directory.EnumerateDirectories(_themesDir)
                .Any(path => Path.GetFileName(path).Equals(assetsDirectoryName, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeThemePackageName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidDataException("The theme package name is empty.");
        }

        StringBuilder builder = new();
        foreach (char character in name.Trim())
        {
            bool invalid = char.IsControl(character)
                || "<>:\"/\\|?*".Contains(character)
                || Path.GetInvalidFileNameChars().Contains(character);
            _ = builder.Append(invalid ? '_' : character);
        }

        string safeName = builder.ToString().Trim().TrimEnd('.', ' ');
        if (safeName.Length > 64)
        {
            safeName = safeName[..64].TrimEnd('.', ' ');
        }

        if (string.IsNullOrWhiteSpace(safeName) || safeName is "." or "..")
        {
            throw new InvalidDataException("The theme package name cannot be used as a file name.");
        }

        string reservedName = safeName.Split('.')[0];
        if (reservedName.Equals("CON", StringComparison.OrdinalIgnoreCase)
            || reservedName.Equals("PRN", StringComparison.OrdinalIgnoreCase)
            || reservedName.Equals("AUX", StringComparison.OrdinalIgnoreCase)
            || reservedName.Equals("NUL", StringComparison.OrdinalIgnoreCase)
            || IsNumberedDeviceName(reservedName, "COM")
            || IsNumberedDeviceName(reservedName, "LPT"))
        {
            safeName = $"_{safeName}";
        }

        return safeName;
    }

    private static bool IsNumberedDeviceName(string value, string prefix)
    {
        return value.Length == prefix.Length + 1
            && value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            && value[^1] is >= '1' and <= '9';
    }

    private static string GetContainedAssetPath(string baseDirectory, string relativeAssetPath)
    {
        if (string.IsNullOrWhiteSpace(relativeAssetPath)
            || relativeAssetPath.StartsWith('/')
            || relativeAssetPath.Contains('\\')
            || relativeAssetPath.Contains(':')
            || relativeAssetPath.Contains('\0'))
        {
            throw new InvalidDataException("Theme package asset paths must be relative.");
        }

        string[] segments = relativeAssetPath.Split('/');
        if (segments.Any(segment => string.IsNullOrWhiteSpace(segment) || segment is "." or ".."))
        {
            throw new InvalidDataException("The theme package contains an unsafe asset path.");
        }

        string fullRoot = Path.GetFullPath(baseDirectory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        string fullPath = Path.GetFullPath(Path.Combine(
            fullRoot,
            Path.Combine(segments)));

        if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("The theme package asset resolves outside its theme directory.");
        }

        return fullPath;
    }

    private static byte[] ReadThemeAsset(string path)
    {
        const int maxAssetBytes = 8 * 1024 * 1024;

        using FileStream source = File.OpenRead(path);
        if (source.Length > maxAssetBytes)
        {
            throw new InvalidDataException("The theme background image exceeds the package asset size limit.");
        }

        using MemoryStream destination = new();
        byte[] buffer = new byte[81920];
        int bytesRead;

        while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
        {
            if (destination.Length + bytesRead > maxAssetBytes)
            {
                throw new InvalidDataException("The theme background image exceeds the package asset size limit.");
            }

            destination.Write(buffer, 0, bytesRead);
        }

        return destination.ToArray();
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
