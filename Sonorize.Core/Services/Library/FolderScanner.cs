using Sonorize.Core.Models;
using Sonorize.Core.Settings;

namespace Sonorize.Core.Services.Library;

public class FolderScanner
{
    private readonly IMusicLibraryService _musicLibrary;
    private readonly SonorizeSettings _settings;

    public FolderScanner(IMusicLibraryService musicLibrary, SonorizeSettings settings)
    {
        _musicLibrary = musicLibrary;
        _settings = settings;
    }

    public async Task<(List<Song> Songs, List<Playlist> Playlists)> ScanAsync(string path)
    {
        var songs = await _musicLibrary.LoadSongsFromFolderAsync(path, _settings.Library.SupportedFileExtensions, CancellationToken.None);
        var playlists = await _musicLibrary.LoadPlaylistsFromFolderAsync(path, CancellationToken.None);
        return (songs, playlists);
    }
}