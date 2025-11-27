using Sonorize.Core.Settings;

namespace Sonorize.Core.Services.Library;

public class LibraryScanner
{
    private readonly IMusicLibraryService _musicLibrary;
    private readonly SonorizeSettings _settings;

    public LibraryScanner(IMusicLibraryService musicLibrary, SonorizeSettings settings)
    {
        _musicLibrary = musicLibrary;
        _settings = settings;
    }

    public async Task<ScanResult> ScanAllFoldersAsync()
    {
        var result = new ScanResult();

        foreach (string path in _settings.Library.MusicFolderPaths)
        {
            var folderSongs = await _musicLibrary.LoadSongsFromFolderAsync(path, _settings.Library.SupportedFileExtensions, CancellationToken.None);
            var folderPlaylists = await _musicLibrary.LoadPlaylistsFromFolderAsync(path, CancellationToken.None);

            result.Songs.AddRange(folderSongs);
            result.Playlists.AddRange(folderPlaylists);
        }

        return result;
    }

    public async Task<ScanResult> ScanSingleFolderAsync(string path)
    {
        var result = new ScanResult();

        var folderSongs = await _musicLibrary.LoadSongsFromFolderAsync(path, _settings.Library.SupportedFileExtensions, CancellationToken.None);
        var folderPlaylists = await _musicLibrary.LoadPlaylistsFromFolderAsync(path, CancellationToken.None);

        result.Songs.AddRange(folderSongs);
        result.Playlists.AddRange(folderPlaylists);

        return result;
    }
}
