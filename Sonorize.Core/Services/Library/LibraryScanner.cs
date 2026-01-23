using Sonorize.Core.Models;
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
        ScanResult result = new();

        foreach (string path in _settings.Library.MusicFolderPaths)
        {
            List<Song> folderSongs = await _musicLibrary.LoadSongsFromFolderAsync(path, _settings.Library.SupportedFileExtensions, CancellationToken.None);
            List<Playlist> folderPlaylists = await _musicLibrary.LoadPlaylistsFromFolderAsync(path, CancellationToken.None);

            result.Songs.AddRange(folderSongs);
            result.Playlists.AddRange(folderPlaylists);
        }

        return result;
    }

    public async Task<ScanResult> ScanSingleFolderAsync(string path)
    {
        ScanResult result = new();

        List<Song> folderSongs = await _musicLibrary.LoadSongsFromFolderAsync(path, _settings.Library.SupportedFileExtensions, CancellationToken.None);
        List<Playlist> folderPlaylists = await _musicLibrary.LoadPlaylistsFromFolderAsync(path, CancellationToken.None);

        result.Songs.AddRange(folderSongs);
        result.Playlists.AddRange(folderPlaylists);

        return result;
    }
}
