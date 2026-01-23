using Sonorize.Core.Models;

namespace Sonorize.Core.Services.Library;

public class LibraryService : IDisposable
{
    private readonly LibraryDataManager _dataManager;
    private readonly LibraryScanCoordinator _scanCoordinator;
    private readonly LibraryEventCoordinator _eventCoordinator;
    private readonly DemoDataLoader _demoDataLoader;
    private readonly PlaylistManager _playlistManager;
    private readonly IMusicLibraryService _musicLibrary;
    private readonly SearchService _searchService;

    public IReadOnlyList<Song> AllSongs => _dataManager.AllSongs;
    public IReadOnlyList<AlbumGroup> AllAlbums => _dataManager.AllAlbums;
    public IReadOnlyList<ArtistGroup> AllArtists => _dataManager.AllArtists;
    public IReadOnlyList<FolderNode> FolderRootNodes => _dataManager.FolderRootNodes;
    public IReadOnlyList<Playlist> FilePlaylists => _dataManager.FilePlaylists;

    public IEnumerable<Playlist> AllPlaylists => _playlistManager.ManualPlaylists.Concat(FilePlaylists);

    public event Action? LibraryChanged
    {
        add => _dataManager.DataUpdated += value;
        remove => _dataManager.DataUpdated -= value;
    }

    public LibraryService(
        LibraryDataManager dataManager,
        LibraryScanCoordinator scanCoordinator,
        LibraryEventCoordinator eventCoordinator,
        DemoDataLoader demoDataLoader,
        PlaylistManager playlistManager,
        IMusicLibraryService musicLibrary,
        SearchService searchService)
    {
        _dataManager = dataManager;
        _scanCoordinator = scanCoordinator;
        _eventCoordinator = eventCoordinator;
        _demoDataLoader = demoDataLoader;
        _playlistManager = playlistManager;
        _musicLibrary = musicLibrary;
        _searchService = searchService;
    }

    public Song? GetSong(string path)
    {
        return _dataManager.GetSong(path);
    }

    public async Task InitializeAsync()
    {
        _eventCoordinator.StartFileMonitoring();
        await _dataManager.LoadCacheIntoMemoryAsync();
    }

    public Task RefreshLibraryAsync()
    {
        _eventCoordinator.StartFileMonitoring();
        return _scanCoordinator.ScanAllFoldersAsync();
    }

    public Task RescanLibraryFullAsync()
    {
        return _scanCoordinator.ScanAllFoldersAsync();
    }

    public Task ScanFolder(string path)
    {
        return _scanCoordinator.ScanSingleFolderAsync(path);
    }

    public void LoadDemoData()
    {
        _demoDataLoader.Load();
    }

    public Playlist CreatePlaylist(string name)
    {
        return _playlistManager.CreatePlaylist(name);
    }

    public void SavePlaylist(Playlist playlist)
    {
        _playlistManager.SavePlaylist(playlist);
    }

    public void DeletePlaylist(Playlist playlist)
    {
        _playlistManager.DeletePlaylist(playlist);
    }

    public void AddSongToPlaylist(Playlist playlist, Song song)
    {
        _playlistManager.AddSongToPlaylist(playlist, song);
    }

    public SongMetadata? GetSongMetadata(Song song)
    {
        return _musicLibrary.GetMetadata(song.FilePath);
    }

    public bool UpdateSongMetadata(Song song, SongMetadata metadata)
    {
        bool success = _musicLibrary.SaveMetadata(song.FilePath, metadata);
        if (success)
        {
            song.Title = metadata.Title;
            song.Artist = metadata.Artist;
            song.Album = metadata.Album;
            _dataManager.AddOrUpdateSong(song);
        }
        return success;
    }

    public Task<IReadOnlyList<Song>> SearchAsync(string query)
    {
        return Task.Run(() => _searchService.Search(_dataManager.AllSongs, query));
    }

    public void Dispose()
    {
        _eventCoordinator?.Dispose();
    }
}