using System.Diagnostics;
using Sonorize.Core.Core;
using Sonorize.Core.Playlists;
using Sonorize.Core.Settings;

namespace Sonorize.Core.Library;

public class LibraryScanCoordinator
{
    private readonly FolderScanner _scanner;
    private readonly LibraryDataManager _dataManager;
    private readonly SonorizeSettings _settings;
    private readonly LogService _logger;

    public LibraryScanCoordinator(
        FolderScanner scanner,
        LibraryDataManager dataManager,
        SonorizeSettings settings,
        LogService logger)
    {
        _scanner = scanner;
        _dataManager = dataManager;
        _settings = settings;
        _logger = logger;
    }

    public async Task ScanAllFoldersAsync()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        IReadOnlyList<string> folders = _settings.Library.MusicFolderPaths;

        _logger.Info($"Library scan started for {folders.Count} configured folder(s).");

        foreach (string path in folders)
        {
            await ScanFolderWithCleanupAsync(path);
        }

        _logger.Info($"Library scan finished in {stopwatch.Elapsed.TotalSeconds:F1}s.");
    }

    public async Task ScanSingleFolderAsync(string path)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        _logger.Info($"Scanning library folder: {path}");

        (List<Song> songs, List<Playlist> playlists) = await _scanner.ScanAsync(path);
        ProcessScanResults(songs, playlists, cleanup: false);

        _logger.Info(
            $"Folder scan completed: {path} ({songs.Count} songs, {playlists.Count} playlists, {stopwatch.Elapsed.TotalSeconds:F1}s).");
    }

    private async Task ScanFolderWithCleanupAsync(string path)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        if (!Directory.Exists(path))
        {
            _logger.Warn($"Configured music folder does not exist and will be skipped: {path}");
            return;
        }

        _logger.Info($"Scanning library folder: {path}");

        (List<Song> songs, List<Playlist> playlists) = await _scanner.ScanAsync(path);
        ProcessScanResults(songs, playlists, cleanup: true);

        _logger.Info(
            $"Folder scan completed: {path} ({songs.Count} songs, {playlists.Count} playlists, {stopwatch.Elapsed.TotalSeconds:F1}s).");
    }

    private void ProcessScanResults(List<Song> songs, List<Playlist> playlists, bool cleanup)
    {
        foreach (Song song in songs)
        {
            _dataManager.AddOrUpdateSong(song);
        }

        if (cleanup)
        {
            RemoveMissingSongs(songs);
        }

        _dataManager.UpdateFilePlaylists(playlists, incremental: !cleanup);
    }

    private void RemoveMissingSongs(List<Song> foundSongs)
    {
        HashSet<string> foundPaths = new(foundSongs.Select(song => song.FilePath), StringComparer.OrdinalIgnoreCase);

        foreach (Song song in _dataManager.AllSongs)
        {
            bool isDemo = song.FilePath.StartsWith(DemoDataGenerator.DemoScheme, StringComparison.OrdinalIgnoreCase);

            if (isDemo || foundPaths.Contains(song.FilePath))
            {
                continue;
            }

            _dataManager.RemoveSong(song.FilePath);
        }
    }
}
