using System.Collections.Concurrent;
using Sonorize.Core.Core;
using Sonorize.Core.Playlists;

namespace Sonorize.Core.Library;

public class LibraryDataManager : IDisposable
{
    private readonly LibraryCacheService _cacheService;
    private readonly LibraryAggregator _aggregator;
    private readonly FolderTreeBuilder _treeBuilder;
    private readonly PlaylistSyncOrchestrator _playlistSync;
    private readonly DataRebuildScheduler _rebuildScheduler;

    // Source of truth for the library, keyed by file path. AllSongs, AllAlbums,
    // AllArtists and FolderRootNodes are snapshots rebuilt from it.
    private readonly ConcurrentDictionary<string, Song> _songs = new(StringComparer.OrdinalIgnoreCase);

    private List<Playlist> _filePlaylists = [];

    public IReadOnlyList<Song> AllSongs { get; private set; } = [];
    public IReadOnlyList<AlbumGroup> AllAlbums { get; private set; } = [];
    public IReadOnlyList<ArtistGroup> AllArtists { get; private set; } = [];
    public IReadOnlyList<FolderNode> FolderRootNodes { get; private set; } = [];
    public IReadOnlyList<Playlist> FilePlaylists => _filePlaylists;

    public event Action? DataUpdated;

    public LibraryDataManager(
        LibraryCacheService cacheService,
        LibraryAggregator aggregator,
        FolderTreeBuilder treeBuilder,
        PlaylistSyncOrchestrator playlistSync)
    {
        _cacheService = cacheService;
        _aggregator = aggregator;
        _treeBuilder = treeBuilder;
        _playlistSync = playlistSync;
        _rebuildScheduler = new DataRebuildScheduler(RebuildAggregatesAsync);
    }

    public void AddOrUpdateSong(Song song)
    {
        _songs[song.FilePath] = song;
        _rebuildScheduler.ScheduleRebuild();
    }

    public void RemoveSong(string path)
    {
        if (_songs.TryRemove(path, out _))
        {
            _rebuildScheduler.ScheduleRebuild();
        }
    }

    public void ClearData()
    {
        _songs.Clear();
        _filePlaylists = [];
        _rebuildScheduler.ScheduleRebuild(immediate: true);
    }

    public Song? GetSong(string path)
    {
        return _songs.GetValueOrDefault(path);
    }

    public async Task LoadCacheIntoMemoryAsync()
    {
        List<Song> cachedSongs = await _cacheService.LoadCacheAsync();

        foreach (Song song in cachedSongs)
        {
            _songs[song.FilePath] = song;
        }

        RebuildSnapshots();
    }

    public void LoadDemoDataIntoMemory(List<Song> demoSongs, List<Playlist> demoPlaylists)
    {
        _songs.Clear();

        foreach (Song song in demoSongs)
        {
            _songs[song.FilePath] = song;
        }

        _filePlaylists = demoPlaylists;

        RebuildSnapshots();
    }

    public void UpdateFilePlaylists(List<Playlist> found, bool incremental = false)
    {
        PlaylistSyncOrchestrator.SyncMode mode = incremental
            ? PlaylistSyncOrchestrator.SyncMode.Incremental
            : PlaylistSyncOrchestrator.SyncMode.Full;

        _filePlaylists = PlaylistSyncOrchestrator.Sync([.. _filePlaylists], found, mode);

        DataUpdated?.Invoke();
    }

    private async Task RebuildAggregatesAsync()
    {
        RebuildSnapshots();
        await _cacheService.SaveCacheAsync(AllSongs);
    }

    private void RebuildSnapshots()
    {
        List<Song> songs = [.. _songs.Values];
        (List<AlbumGroup>? albums, List<ArtistGroup>? artists) = _aggregator.Aggregate(songs);
        List<FolderNode> folderTree = _treeBuilder.Build(songs);

        AllSongs = [.. songs.OrderBy(s => s.Title, StringComparer.OrdinalIgnoreCase)];
        AllAlbums = albums;
        AllArtists = artists;
        FolderRootNodes = folderTree;

        DataUpdated?.Invoke();
    }

    public void Dispose()
    {
        _rebuildScheduler.Dispose();
    }
}
