using Sonorize.Core.Models;

namespace Sonorize.Core.Services.Library;

public class PlaylistManager
{
    private readonly PlaylistPersistenceService _persistence;
    private readonly List<Playlist> _manualPlaylists;

    public IReadOnlyList<Playlist> ManualPlaylists => _manualPlaylists;

    // Event to notify parent/coordinator (LibraryService) of changes
    public event Action? PlaylistsChanged;

    public PlaylistManager(PlaylistPersistenceService persistence)
    {
        _persistence = persistence;
        _manualPlaylists = _persistence.LoadPlaylists();
    }

    public Playlist CreatePlaylist(string name)
    {
        var playlist = new Playlist
        {
            Name = name,
            Type = PlaylistType.Manual,
            Id = Guid.NewGuid()
        };

        _manualPlaylists.Add(playlist);
        _persistence.SavePlaylist(playlist);

        PlaylistsChanged?.Invoke();
        return playlist;
    }

    public void SavePlaylist(Playlist playlist)
    {
        if (playlist.Type == PlaylistType.Manual)
        {
            _persistence.SavePlaylist(playlist);
            PlaylistsChanged?.Invoke();
        }
    }

    public void DeletePlaylist(Playlist playlist)
    {
        if (playlist.Type == PlaylistType.Manual)
        {
            if (_manualPlaylists.Remove(playlist))
            {
                _persistence.DeletePlaylist(playlist);
                PlaylistsChanged?.Invoke();
            }
        }
    }

    public void AddSongToPlaylist(Playlist playlist, Song song)
    {
        if (playlist.Type == PlaylistType.Manual && !playlist.SongFilePaths.Contains(song.FilePath))
        {
            playlist.SongFilePaths.Add(song.FilePath);
            SavePlaylist(playlist);
        }
    }
}