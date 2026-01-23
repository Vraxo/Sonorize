using Sonorize.Core.Helpers;
using Sonorize.Core.Models;
using Sonorize.Core.Settings;
using System.Collections.Concurrent;

namespace Sonorize.Core.Services.Library;

public class LibraryService : IDisposable
{
    public IReadOnlyList<Song> AllSongs { get; private set; } = [];
    public IReadOnlyList<AlbumGroup> AllAlbums { get; private set; } = [];
    public IReadOnlyList<ArtistGroup> AllArtists { get; private set; } = [];
    public IReadOnlyList<FolderNode> FolderRootNodes { get; private set; } = [];

    private List<Playlist>? _demoPlaylists;

    public IEnumerable<Playlist> AllPlaylists => _demoPlaylists ?? _playlistManager.ManualPlaylists.Concat(FilePlaylists);

    public IReadOnlyList<Playlist> FilePlaylists { get; private set; } = [];

    public event Action? LibraryChanged;

    private readonly LibraryScanner _scanner;
    private readonly IMusicLibraryService _musicLibrary;
    private readonly SonorizeSettings _settings;
    private readonly LibraryCacheService _cacheService;
    private readonly LibraryAggregator _aggregator;
    private readonly LibraryFileMonitor _fileMonitor;
    private readonly PlaylistManager _playlistManager;
    private readonly SearchService _searchService;

    private ConcurrentDictionary<string, Song> _songsByPath = new();

    private readonly ActionDebouncer _rebuildDebouncer = new();

    public LibraryService(
        LibraryScanner scanner,
        IMusicLibraryService musicLibrary,
        SonorizeSettings settings,
        ISettingsManager<SonorizeSettings> settingsManager,
        LibraryCacheService cacheService,
        LibraryAggregator aggregator,
        LibraryFileMonitor fileMonitor,
        PlaylistManager playlistManager,
        SearchService searchService)
    {
        _scanner = scanner;
        _musicLibrary = musicLibrary;
        _settings = settings;
        _cacheService = cacheService;
        _aggregator = aggregator;
        _fileMonitor = fileMonitor;
        _playlistManager = playlistManager;
        _searchService = searchService;

        _playlistManager.PlaylistsChanged += OnPlaylistsChanged;

        _fileMonitor.FileAdded += OnFileAdded;
        _fileMonitor.FileRemoved += OnFileRemoved;
        _fileMonitor.FileRenamed += OnFileRenamed;
    }

    public Song? GetSong(string path)
    {
        return _songsByPath.TryGetValue(path, out Song? song) ? song : null;
    }

    private void OnPlaylistsChanged()
    {
        LibraryChanged?.Invoke();
    }

    public void LoadDemoData()
    {
        _songsByPath.Clear();
        var demoSongs = DemoDataGenerator.Generate(100);

        foreach (var song in demoSongs)
        {
            _songsByPath[song.FilePath] = song;
        }

        _demoPlaylists = DemoDataGenerator.GeneratePlaylists(demoSongs);

        _ = RebuildAggregatesAsync(saveCache: false);
    }

    private async void OnFileAdded(string path)
    {
        if (_songsByPath.ContainsKey(path))
        {
            return;
        }

        var song = await _musicLibrary.CreateSongFromFileAsync(path);
        if (song is not null)
        {
            if (_songsByPath.TryAdd(path, song))
            {
                TriggerDebouncedRebuild();
            }
        }
    }

    private void OnFileRemoved(string path)
    {
        if (_songsByPath.TryRemove(path, out _))
        {
            TriggerDebouncedRebuild();
        }
    }

    private void OnFileRenamed(string oldPath, string newPath)
    {
        if (_songsByPath.TryRemove(oldPath, out Song? song))
        {
            song.FilePath = newPath;
            _ = _songsByPath.TryAdd(newPath, song);
            TriggerDebouncedRebuild();
        }
        else
        {
            OnFileAdded(newPath);
        }
    }

    private void TriggerDebouncedRebuild()
    {
        _rebuildDebouncer.Debounce(async () => await RebuildAggregatesAsync(saveCache: true), 1000);
    }

    public async Task InitializeAsync()
    {
        _fileMonitor.StartWatching();

        var cachedSongs = await _cacheService.LoadCacheAsync();

        if (cachedSongs.Count > 0)
        {
            _songsByPath = new ConcurrentDictionary<string, Song>(cachedSongs.ToDictionary(s => s.FilePath, s => s));
            await RebuildAggregatesAsync(saveCache: false);
        }

        if (_settings.Library.ScanOnStartup || cachedSongs.Count == 0)
        {
            _ = Task.Run(RescanLibraryFullAsync);
        }
    }

    public async Task RefreshLibraryAsync()
    {
        _fileMonitor.StartWatching();
        await RescanLibraryFullAsync();
    }

    public async Task RescanLibraryFullAsync()
    {
        var scanResult = await _scanner.ScanAllFoldersAsync();

        HashSet<string> foundPaths = new(scanResult.Songs.Count, StringComparer.OrdinalIgnoreCase);

        foreach (Song song in scanResult.Songs)
        {
            _songsByPath[song.FilePath] = song;
            _ = foundPaths.Add(song.FilePath);
        }

        foreach (var key in _songsByPath.Keys)
        {
            // Only remove if it's not a demo song
            if (!key.StartsWith("demo://") && !foundPaths.Contains(key))
            {
                _ = _songsByPath.TryRemove(key, out _);
            }
        }

        SyncFilePlaylists(scanResult.Playlists);

        await RebuildAggregatesAsync(saveCache: true);
    }

    public async Task ScanFolder(string path)
    {
        _fileMonitor.StartWatching();

        var scanResult = await _scanner.ScanSingleFolderAsync(path);

        foreach (Song song in scanResult.Songs)
        {
            _songsByPath[song.FilePath] = song;
        }

        UpdateFilePlaylistsIncremental(scanResult.Playlists);

        await RebuildAggregatesAsync(saveCache: true);
    }

    private async Task RebuildAggregatesAsync(bool saveCache)
    {
        await Task.Run(async () =>
        {
            var songs = _songsByPath.Values.ToList();
            var (albums, artists) = _aggregator.Aggregate(songs);
            var folderTree = BuildFolderTree(songs);

            AllSongs = songs.OrderBy(s => s.Title, StringComparer.OrdinalIgnoreCase).ToList();
            AllAlbums = albums;
            AllArtists = artists;
            FolderRootNodes = folderTree;

            LibraryChanged?.Invoke();

            if (saveCache)
            {
                await _cacheService.SaveCacheAsync(AllSongs);
            }
        });
    }

    private List<FolderNode> BuildFolderTree(ICollection<Song> songs)
    {
        var rootNodes = new List<FolderNode>();

        // Initialize root nodes from settings, ensuring they exist
        foreach (var rootPath in _settings.Library.MusicFolderPaths.Where(Directory.Exists))
        {
            var rootNode = new FolderNode { Name = Path.GetFileName(rootPath), Path = rootPath };
            rootNodes.Add(rootNode);
        }

        if (rootNodes.Count == 0)
        {
            return [];
        }

        // Process each song
        foreach (var song in songs.Where(s => !s.FilePath.StartsWith("demo://")))
        {
            string? dirPath = Path.GetDirectoryName(song.FilePath);
            if (string.IsNullOrEmpty(dirPath))
            {
                continue;
            }

            // Find which root this song belongs to
            var rootNode = rootNodes.FirstOrDefault(r => dirPath.StartsWith(r.Path, StringComparison.OrdinalIgnoreCase));
            if (rootNode == null)
            {
                continue;
            }

            // Traverse or create nodes from the root down to the song's directory
            string relativePath = dirPath[rootNode.Path.Length..];
            var segments = relativePath.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);

            var currentNode = rootNode;
            foreach (var segment in segments)
            {
                var childNode = currentNode.Children.FirstOrDefault(c => c.Name.Equals(segment, StringComparison.OrdinalIgnoreCase));

                if (childNode == null)
                {
                    string newPath = Path.Combine(currentNode.Path, segment);
                    childNode = new FolderNode { Name = segment, Path = newPath };
                    currentNode.Children.Add(childNode);
                }
                currentNode = childNode;
            }

            // Add song to the final directory node
            currentNode.Songs.Add(song);
        }

        // Recursive sort for all nodes
        static void SortNode(FolderNode node)
        {
            node.Children = [.. node.Children.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)];
            node.Songs = [.. node.Songs.OrderBy(s => s.Title, StringComparer.OrdinalIgnoreCase)];
            foreach (var child in node.Children)
            {
                SortNode(child);
            }
        }

        foreach (var root in rootNodes)
        {
            SortNode(root);
        }

        return rootNodes.OrderBy(n => n.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private void SyncFilePlaylists(List<Playlist> foundPlaylists)
    {
        var oldMap = FilePlaylists.ToDictionary(p => p.FilePath!, StringComparer.OrdinalIgnoreCase);
        var newList = new List<Playlist>(foundPlaylists.Count);

        foreach (Playlist found in foundPlaylists)
        {
            if (string.IsNullOrEmpty(found.FilePath))
            {
                continue;
            }

            if (oldMap.TryGetValue(found.FilePath, out Playlist? existing))
            {
                existing.Name = found.Name;
                existing.SongFilePaths = found.SongFilePaths;
                newList.Add(existing);
            }
            else
            {
                newList.Add(found);
            }
        }

        FilePlaylists = newList;
    }

    private void UpdateFilePlaylistsIncremental(List<Playlist> newOrUpdated)
    {
        var currentList = FilePlaylists.ToList();
        var map = currentList.ToDictionary(p => p.FilePath!, StringComparer.OrdinalIgnoreCase);
        bool changed = false;

        foreach (Playlist found in newOrUpdated)
        {
            if (string.IsNullOrEmpty(found.FilePath))
            {
                continue;
            }

            if (map.TryGetValue(found.FilePath, out Playlist? existing))
            {
                existing.Name = found.Name;
                existing.SongFilePaths = found.SongFilePaths;
            }
            else
            {
                currentList.Add(found);
                changed = true;
            }
        }

        if (changed || newOrUpdated.Count > 0)
        {
            FilePlaylists = currentList;
        }
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
            _ = RebuildAggregatesAsync(saveCache: true);
        }
        return success;
    }

    public async Task<IReadOnlyList<Song>> SearchAsync(string query)
    {
        return await Task.Run(() => _searchService.Search(AllSongs, query));
    }

    public void Dispose()
    {
        _rebuildDebouncer.Dispose();
        _playlistManager.PlaylistsChanged -= OnPlaylistsChanged;
        _fileMonitor.FileAdded -= OnFileAdded;
        _fileMonitor.FileRemoved -= OnFileRemoved;
        _fileMonitor.FileRenamed -= OnFileRenamed;
        _fileMonitor.Dispose();
    }
}