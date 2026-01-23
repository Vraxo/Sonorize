using Sonorize.Core.Helpers;
using Sonorize.Core.Models;
using Sonorize.Core.Settings;

namespace Sonorize.Core.Services.Library;

public class LibraryScanCoordinator
{
    private readonly FolderScanner _scanner;
    private readonly LibraryDataManager _dataManager;
    private readonly SonorizeSettings _settings;

    public LibraryScanCoordinator(
        FolderScanner scanner,
        LibraryDataManager dataManager,
        SonorizeSettings settings)
    {
        _scanner = scanner;
        _dataManager = dataManager;
        _settings = settings;
    }

    public async Task ScanAllFoldersAsync()
    {
        foreach (string path in _settings.Library.MusicFolderPaths)
        {
            await ScanFolderWithCleanupAsync(path);
        }
    }

    public async Task ScanSingleFolderAsync(string path)
    {
        var (songs, playlists) = await _scanner.ScanAsync(path);
        ProcessScanResults(songs, playlists, cleanup: false);
    }

    private async Task ScanFolderWithCleanupAsync(string path)
    {
        var (songs, playlists) = await _scanner.ScanAsync(path);
        ProcessScanResults(songs, playlists, cleanup: true);
    }

    private void ProcessScanResults(List<Song> songs, List<Playlist> playlists, bool cleanup)
    {
        foreach (var song in songs)
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
        var foundPaths = new HashSet<string>(foundSongs.Select(s => s.FilePath), StringComparer.OrdinalIgnoreCase);

        foreach (var song in _dataManager.AllSongs)
        {
            bool isDemo = song.FilePath.StartsWith(DemoDataGenerator.DemoScheme, StringComparison.OrdinalIgnoreCase);
            if (!isDemo && !foundPaths.Contains(song.FilePath))
            {
                _dataManager.RemoveSong(song.FilePath);
            }
        }
    }
}