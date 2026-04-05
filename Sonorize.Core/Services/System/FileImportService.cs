using Sonorize.Core.Models;
using Sonorize.Core.Services.Library;
using Sonorize.Core.Settings;

namespace Sonorize.Core.Services.System;

public class FileImportService
{
    private readonly IMusicLibraryService _libraryService;
    private readonly SonorizeSettings _settings;
    private readonly string _cacheDir;

    public FileImportService(IMusicLibraryService libraryService, SonorizeSettings settings)
    {
        _libraryService = libraryService;
        _settings = settings;
        _cacheDir = Path.Combine(Path.GetTempPath(), "Sonorize", "DropCache");

        if (Directory.Exists(_cacheDir))
        {
            return;
        }

        _ = Directory.CreateDirectory(_cacheDir);
    }

    public async Task<List<Song>> ImportStreamsAsync(IEnumerable<(Stream stream, string fileName)> files)
    {
        // Cleanup old cache on new drop to prevent infinite bloat
        CleanupCache();

        List<Song> songs = [];

        foreach ((Stream? stream, string? fileName) in files)
        {
            string ext = Path.GetExtension(fileName).ToLowerInvariant();

            if (!_settings.Library.SupportedFileExtensions.Contains(ext))
            {
                continue;
            }

            string tempPath = Path.Combine(_cacheDir, fileName);

            try
            {
                using (FileStream fs = new(tempPath, FileMode.Create))
                {
                    await stream.CopyToAsync(fs);
                }

                Song? song = await _libraryService.CreateSongFromFileAsync(tempPath);

                if (song is null)
                {
                    continue;
                }

                if (song.Artist == "Unknown Artist")
                {
                    song.Artist = "Dropped File";
                }

                if (song.Album == "Unknown Album")
                {
                    song.Album = "Queue";
                }

                songs.Add(song);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Import] Failed to process {fileName}: {ex.Message}");
            }
        }

        return songs;
    }

    private void CleanupCache()
    {
        try
        {
            DirectoryInfo dir = new(_cacheDir);

            foreach (FileInfo file in dir.GetFiles())
            {
                file.Delete();
            }
        }
        catch { /* Ignore locks */ }
    }
}