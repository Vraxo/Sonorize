using Sonorize.Core.Models;

namespace Sonorize.Core.Services.Library;

public class PlaylistManager
{
    private readonly PlaylistPersistenceService _persistence;
    private readonly List<Playlist> _manualPlaylists;

    public IReadOnlyList<Playlist> ManualPlaylists => _manualPlaylists;

    public event Action? PlaylistsChanged;

    public PlaylistManager(PlaylistPersistenceService persistence)
    {
        _persistence = persistence;
        _manualPlaylists = _persistence.LoadPlaylists();
    }

    public Playlist CreatePlaylist(string name)
    {
        Playlist playlist = new()
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
        if (playlist.Type != PlaylistType.Manual)
        {
            return;
        }

        _persistence.SavePlaylist(playlist);
        PlaylistsChanged?.Invoke();
    }

    public void DeletePlaylist(Playlist playlist)
    {
        if (playlist.Type != PlaylistType.Manual || !_manualPlaylists.Remove(playlist))
        {
            return;
        }

        _persistence.DeletePlaylist(playlist);
        PlaylistsChanged?.Invoke();
    }

    public void AddSongToPlaylist(Playlist playlist, Song song)
    {
        if (playlist.Type != PlaylistType.Manual || playlist.SongFilePaths.Contains(song.FilePath))
        {
            return;
        }

        playlist.SongFilePaths.Add(song.FilePath);
        SavePlaylist(playlist);
    }
}