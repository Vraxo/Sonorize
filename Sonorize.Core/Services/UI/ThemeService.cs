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
            // Look for themes shipped with the app (bin/Themes)
            string builtInThemesDir = Path.Combine(AppContext.BaseDirectory, "Themes");

            if (!Directory.Exists(builtInThemesDir))
            {
                return;
            }

            foreach (string file in Directory.EnumerateFiles(builtInThemesDir, "*.json"))
            {
                string fileName = Path.GetFileName(file);
                string dest = Path.Combine(_themesDir, fileName);

                // Only copy if it doesn't exist to preserve user customizations
                // or if they deleted it intentionally? 
                // Decision: We treat these as "starter templates". If missing, we restore them.
                // This allows users to "reset" a theme by deleting it from AppData.
                if (!File.Exists(dest))
                {
                    File.Copy(file, dest);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ThemeService] Failed to ensure built-in themes: {ex.Message}");
        }
    }

    public List<string> GetAvailableThemes()
    {
        return !Directory.Exists(_themesDir)
            ? []
            : Directory.EnumerateFiles(_themesDir, "*.json")
                        .Select(Path.GetFileNameWithoutExtension)
                        .Where(x => x != null)
                        .OrderBy(x => x)
                        .ToList()!;
    }

    public void SaveTheme(string name, SonorizeTheme theme)
    {
        string safeName = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
        string path = Path.Combine(_themesDir, $"{safeName}.json");
        string json = JsonSerializer.Serialize(theme, _jsonOptions);
        File.WriteAllText(path, json);
    }

    public SonorizeTheme? LoadTheme(string name)
    {
        string path = Path.Combine(_themesDir, $"{name}.json");

        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<SonorizeTheme>(json, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public void DeleteTheme(string name)
    {
        string path = Path.Combine(_themesDir, $"{name}.json");

        if (!File.Exists(path))
        {
            return;
        }

        File.Delete(path);
    }
}