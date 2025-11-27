using Sonorize.Core.Helpers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sonorize.Core.Settings;

public class SettingsManager<T> : ISettingsManager<T> where T : class, new()
{
    private readonly string _filePath;
    private readonly string _settingsDirectory;

    public event Action? SettingsSaved;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public SettingsManager(string fileName)
        : this(fileName, AppDataHelper.GetBaseDirectory())
    {
    }

    // Internal constructor for testing purposes
    internal SettingsManager(string fileName, string basePath)
    {
        _settingsDirectory = basePath;
        _filePath = Path.Combine(_settingsDirectory, fileName);
    }

    public void Save(T settings)
    {
        try
        {
            _ = Directory.CreateDirectory(_settingsDirectory);
            string json = JsonSerializer.Serialize(settings, SerializerOptions);
            File.WriteAllText(_filePath, json);

            // Notify subscribers
            SettingsSaved?.Invoke();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving settings to '{_filePath}': {ex.Message}");
        }
    }

    public T Load()
    {
        if (!File.Exists(_filePath))
        {
            T newSettings = new();
            Save(newSettings); // Create the file on first load
            return newSettings;
        }

        try
        {
            string json = File.ReadAllText(_filePath);
            T? settings = JsonSerializer.Deserialize<T>(json, SerializerOptions);
            return settings ?? new T();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading settings from '{_filePath}'. Using defaults. Error: {ex.Message}");
            return new T();
        }
    }

    public void Delete()
    {
        if (File.Exists(_filePath))
        {
            try { File.Delete(_filePath); } catch { }
        }
    }
}