using Sonorize.Core.Helpers;
using Sonorize.Core.Models;
using System.Text.Json;

namespace Sonorize.Core.Services.Audio;

public class EqPresetService
{
    private readonly string _presetsDir;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public EqPresetService()
    {
        _presetsDir = AppDataHelper.GetSubDirectory("EqPresets");
        EnsureDefaults();
    }

    public List<EqPreset> LoadPresets()
    {
        var presets = new List<EqPreset>();

        if (!Directory.Exists(_presetsDir))
        {
            return presets;
        }

        foreach (string file in Directory.EnumerateFiles(_presetsDir, "*.json"))
        {
            try
            {
                string json = File.ReadAllText(file);
                var preset = JsonSerializer.Deserialize<EqPreset>(json);
                if (preset != null)
                {
                    // Ensure name matches filename to prevent drift, or trust file content?
                    // Trusting content is safer for rename operations, but let's ensure consistency.
                    // For now, we return what's in the file.
                    presets.Add(preset);
                }
            }
            catch { /* Ignore corrupt files */ }
        }

        // Always ensure defaults exist in the returned list (in case they were deleted manually from disk)
        if (!presets.Any(p => p.IsDefault))
        {
            EnsureDefaults();
            // Re-load to get them
            return LoadPresets();
        }

        return presets.OrderBy(p => p.Name).ToList();
    }

    public void SavePreset(EqPreset preset)
    {
        string safeName = string.Join("_", preset.Name.Split(Path.GetInvalidFileNameChars()));
        string path = Path.Combine(_presetsDir, $"{safeName}.json");
        string json = JsonSerializer.Serialize(preset, _jsonOptions);
        File.WriteAllText(path, json);
    }

    public void DeletePreset(EqPreset preset)
    {
        if (preset.IsDefault)
        {
            return; // Guard clause
        }

        string safeName = string.Join("_", preset.Name.Split(Path.GetInvalidFileNameChars()));
        string path = Path.Combine(_presetsDir, $"{safeName}.json");

        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private void EnsureDefaults()
    {
        var defaults = new List<EqPreset>
        {
            new() { Name = "Flat", IsDefault = true, Gains = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0] },
            new() { Name = "Bass Boost", IsDefault = true, Gains = [6, 5, 4, 2, 0, 0, 0, 0, 0, 0] },
            new() { Name = "Treble Boost", IsDefault = true, Gains = [0, 0, 0, 0, 0, 1, 2, 4, 5, 6] },
            new() { Name = "Rock", IsDefault = true, Gains = [4, 3, 1, -1, -2, -2, -1, 1, 3, 4] },
            new() { Name = "Pop", IsDefault = true, Gains = [2, 1, 3, 2, 0, -1, 0, 1, 2, 1] },
            new() { Name = "Vocal", IsDefault = true, Gains = [-2, -2, -1, 1, 3, 4, 3, 1, 0, -1] },
            new() { Name = "Jazz", IsDefault = true, Gains = [3, 2, 0, 2, -1, -1, 0, 2, 3, 4] },
            new() { Name = "Classical", IsDefault = true, Gains = [4, 3, 2, 1, -1, -1, 0, 2, 3, 3] }
        };

        foreach (var def in defaults)
        {
            // Only write if not exists to avoid overwriting if user somehow modified a default file (though they shouldn't)
            // But since they are defaults, we enforce them.
            SavePreset(def);
        }
    }
}