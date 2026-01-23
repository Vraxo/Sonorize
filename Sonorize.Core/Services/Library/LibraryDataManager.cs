using Sonorize.Core.Models;

namespace Sonorize.Core.Services.Library;

public class LibraryDataManager : IDisposable
{
    private readonly LibraryCacheService _cacheService;
    private readonly LibraryAggregator _aggregator;
    private readonly FolderTreeBuilder _treeBuilder;
    private readonly PlaylistSyncOrchestrator _playlistSync;
    private readonly DataRebuildScheduler _rebuildScheduler;

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
        _rebuildScheduler.ScheduleRebuild();
    }

    public void RemoveSong(string path)
    {
        _rebuildScheduler.ScheduleRebuild();
    }

    public void ClearData()
    {
        _rebuildScheduler.ScheduleRebuild(immediate: true);
    }

    public Song? GetSong(string path)
    {
        var songs = AllSongs;
        return songs.FirstOrDefault(s => s.FilePath.Equals(path, StringComparison.OrdinalIgnoreCase));
    }

    public async Task LoadCacheIntoMemoryAsync()
    {
        var cachedSongs = await _cacheService.LoadCacheAsync();
        AllSongs = cachedSongs;
        DataUpdated?.Invoke();
    }

    public void LoadDemoDataIntoMemory(List<Song> demoSongs, List<Playlist> demoPlaylists)
    {
        AllSongs = demoSongs;
        _filePlaylists = demoPlaylists;
        DataUpdated?.Invoke();
    }

    public void UpdateFilePlaylists(List<Playlist> found, bool incremental = false)
    {
        var mode = incremental ? PlaylistSyncOrchestrator.SyncMode.Incremental : PlaylistSyncOrchestrator.SyncMode.Full;
        _filePlaylists = _playlistSync.Sync(_filePlaylists.ToList(), found, mode);
        DataUpdated?.Invoke();
    }

    private async Task RebuildAggregatesAsync()
    {
        var songs = AllSongs.ToList();
        var (albums, artists) = _aggregator.Aggregate(songs);
        var folderTree = _treeBuilder.Build(songs);

        AllSongs = songs.OrderBy(s => s.Title, StringComparer.OrdinalIgnoreCase).ToList();
        AllAlbums = albums;
        AllArtists = artists;
        FolderRootNodes = folderTree;

        DataUpdated?.Invoke();
        await _cacheService.SaveCacheAsync(AllSongs);
    }

    public void Dispose()
    {
        _rebuildScheduler.Dispose();
    }
}