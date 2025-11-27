using NSubstitute;
using Sonorize.Core.Models;
using Sonorize.Core.Services.Library;
using Sonorize.Core.Settings;

namespace Sonorize.Core.Tests;

public class LibraryServiceTests
{
    private class MockMusicLibraryService : IMusicLibraryService
    {
        public List<Song> SongsToReturn { get; set; } = [];
        public List<Playlist> PlaylistsToReturn { get; set; } = [];

        public Task<List<Playlist>> LoadPlaylistsFromFolderAsync(string folderPath, CancellationToken cancellationToken)
        {
            return Task.FromResult(PlaylistsToReturn.ToList());
        }

        public Task<List<Song>> LoadSongsFromFolderAsync(string folderPath, IEnumerable<string> extensions, CancellationToken cancellationToken)
        {
            return Task.FromResult(SongsToReturn.ToList());
        }

        public SongMetadata? GetMetadata(string filePath)
        {
            return null;
        }

        public bool SaveMetadata(string filePath, SongMetadata metadata)
        {
            return false;
        }

        public Task<Song?> CreateSongFromFileAsync(string filePath)
        {
            return Task.FromResult<Song?>(null);
        }
    }

    private readonly MockMusicLibraryService _mockMusicLibrary;
    private readonly SonorizeSettings _settings;
    private readonly LibraryService _libraryService;

    // Dependencies
    private readonly LibraryFileMonitor _fileMonitor;
    private readonly PlaylistManager _playlistManager;
    private readonly SearchService _searchService;

    private readonly string _mockMusicPath;

    public LibraryServiceTests()
    {
        _mockMusicLibrary = new MockMusicLibraryService();
        _settings = new SonorizeSettings();

        string tempDir = Path.Combine(Path.GetTempPath(), "SonorizeTest_" + Guid.NewGuid());
        _ = Directory.CreateDirectory(tempDir);

        var settingsManager = new SettingsManager<SonorizeSettings>("test_lib_settings.json", tempDir);
        var playlistPersistence = new PlaylistPersistenceService(Path.Combine(tempDir, "Playlists"));
        var cacheService = new LibraryCacheService(tempDir);
        var aggregator = new LibraryAggregator();

        // New Dependencies
        _fileMonitor = Substitute.For<LibraryFileMonitor>(_settings);
        _playlistManager = new PlaylistManager(playlistPersistence);
        _searchService = new SearchService();

        var scanner = new LibraryScanner(_mockMusicLibrary, _settings);

        _libraryService = new LibraryService(
            scanner,
            _mockMusicLibrary,
            _settings,
            settingsManager,
            cacheService,
            aggregator,
            _fileMonitor,
            _playlistManager,
            _searchService);

        // Safe platform-agnostic path construction
        _mockMusicPath = Path.Combine(tempDir, "Music");

        _sampleSongs =
        [
            new Song { FilePath = Path.Combine(_mockMusicPath, "song1.mp3"), Title = "Alpha Track", Artist = "Artist A", Album = "Album X" },
            new Song { FilePath = Path.Combine(_mockMusicPath, "song2.mp3"), Title = "Beta Song", Artist = "Artist B", Album = "Album Y" },
            new Song { FilePath = Path.Combine(_mockMusicPath, "song3.mp3"), Title = "Gamma Hit", Artist = "Artist C", Album = "Album X" },
            new Song { FilePath = Path.Combine(_mockMusicPath, "song4.mp3"), Title = "Another Alpha", Artist = "Artist D", Album = "Album Z" }
        ];
    }

    private readonly List<Song> _sampleSongs;

    [Fact]
    public async Task InitialScan_WithOneFolder_PopulatesAllSongs()
    {
        _settings.Library.MusicFolderPaths.Add(_mockMusicPath);
        _mockMusicLibrary.SongsToReturn = _sampleSongs;

        // Use RescanLibraryFullAsync to explicitly wait for the scan in tests
        await _libraryService.RescanLibraryFullAsync();

        Assert.Equal(4, _libraryService.AllSongs.Count);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Search_WithNullOrWhitespaceQuery_ReturnsAllSongs(string? query)
    {
        // Arrange
        _settings.Library.MusicFolderPaths.Add(_mockMusicPath);
        _mockMusicLibrary.SongsToReturn = _sampleSongs;
        await _libraryService.RescanLibraryFullAsync();

        // Act
        IReadOnlyList<Song> results = await _libraryService.SearchAsync(query!);

        // Assert: Empty query implies "Show All", not "Show None"
        Assert.Equal(4, results.Count);
    }

    [Fact]
    public async Task Search_WithQueryMatchingTitle_ReturnsCorrectSongs()
    {
        _settings.Library.MusicFolderPaths.Add(_mockMusicPath);
        _mockMusicLibrary.SongsToReturn = _sampleSongs;
        await _libraryService.RescanLibraryFullAsync();

        IReadOnlyList<Song> results = await _libraryService.SearchAsync("Alpha");

        Assert.Equal(2, results.Count);
        Assert.Contains(results, s => s.Title == "Alpha Track");
        Assert.Contains(results, s => s.Title == "Another Alpha");
    }
}