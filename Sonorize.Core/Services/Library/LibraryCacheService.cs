using Sonorize.Core.Helpers;
using Sonorize.Core.Models;
using System.Text.Json;

namespace Sonorize.Core.Services.Library;

public class LibraryCacheService
{
    private readonly string _cacheFilePath;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false // Compact for speed
    };

    // Parameterless for DI
    public LibraryCacheService() : this(null) { }

    // Overload for testing to isolate the cache file
    public LibraryCacheService(string? basePath)
    {
        string folder = basePath ?? AppDataHelper.GetBaseDirectory();

        if (!Directory.Exists(folder))
        {
            _ = Directory.CreateDirectory(folder);
        }

        _cacheFilePath = Path.Combine(folder, "LibraryCache.json");
    }

    public async Task SaveCacheAsync(IEnumerable<Song> songs)
    {
        try
        {
            using FileStream createStream = File.Create(_cacheFilePath);
            await JsonSerializer.SerializeAsync(createStream, songs, _jsonOptions);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Cache] Failed to save library cache: {ex.Message}");
        }
    }

    public async Task<List<Song>> LoadCacheAsync()
    {
        if (!File.Exists(_cacheFilePath))
        {
            return [];
        }

        try
        {
            using FileStream openStream = File.OpenRead(_cacheFilePath);
            var songs = await JsonSerializer.DeserializeAsync<List<Song>>(openStream, _jsonOptions);
            return songs ?? [];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Cache] Failed to load library cache: {ex.Message}");
            return [];
        }
    }
}