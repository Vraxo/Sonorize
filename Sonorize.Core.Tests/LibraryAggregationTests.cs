using NSubstitute;
using Sonorize.Core.Models;
using Sonorize.Core.Services.Library;
using Sonorize.Core.Settings;

namespace Sonorize.Core.Tests;

public class LibraryAggregationTests
{
    private class MockMusicLibraryService : IMusicLibraryService
    {
        public List<Song> SongsToReturn { get; set; } = [];

        public Task<List<Playlist>> LoadPlaylistsFromFolderAsync(string folderPath, CancellationToken cancellationToken)
        {
            return Task.FromResult(new List<Playlist>());
        }

        public Task<List<Song>> LoadSongsFromFolderAsync(string folderPath, IEnumerable<string> extensions, CancellationToken cancellationToken)
        {
            return Task.FromResult(SongsToReturn);
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
    private readonly LibraryService _libraryService;
    private readonly List<Song> _sampleSongs;

    public LibraryAggregationTests()
    {
        _mockMusicLibrary = new MockMusicLibraryService();
        var settings = new SonorizeSettings();
        var settingsManager = new SettingsManager<SonorizeSettings>("test_agg_settings.json", Path.GetTempPath());

        string tempDir = Path.Combine(Path.GetTempPath(), "SonorizeTest_" + Guid.NewGuid());
        _ = Directory.CreateDirectory(tempDir);

        var playlistPersistence = new PlaylistPersistenceService(Path.Combine(tempDir, "Playlists"));
        var cacheService = new LibraryCacheService(tempDir); // Isolated cache
        var aggregator = new LibraryAggregator();

        // New Dependencies
        var fileMonitor = Substitute.For<LibraryFileMonitor>(settings);
        var playlistManager = new PlaylistManager(playlistPersistence);
        var searchService = new SearchService();
        var scanner = new LibraryScanner(_mockMusicLibrary, settings);

        _libraryService = new LibraryService(
            scanner,
            _mockMusicLibrary,
            settings,
            settingsManager,
            cacheService,
            aggregator,
            fileMonitor,
            playlistManager,
            searchService);

        // Platform-safe mock path
        string musicPath = Path.Combine(tempDir, "Music");
        settings.Library.MusicFolderPaths.Add(musicPath);

        _sampleSongs =
        [
            new Song { FilePath = "1.mp3", Title = "Song 1", Artist = "Artist A", Album = "Album 1", HasArt = true },
            new Song { FilePath = "2.mp3", Title = "Song 2", Artist = "Artist A", Album = "Album 1" },
            new Song { FilePath = "3.mp3", Title = "Song 3", Artist = "Artist A", Album = "Album 2" },
            new Song { FilePath = "4.mp3", Title = "Song 4", Artist = "Artist B", Album = "Album 1" },
            new Song { FilePath = "5.mp3", Title = "Song 5", Artist = "Artist C", Album = "Single" }
        ];

        _mockMusicLibrary.SongsToReturn = _sampleSongs;
    }

    [Fact]
    public async Task InitialScan_AutomaticallyGeneratesAggregates()
    {
        // Arrange
        // In tests, we use RescanLibraryFullAsync explicitly to ensure completion before assertion
        // (InitializeAsync triggers background tasks which are hard to test)
        await _libraryService.RescanLibraryFullAsync();

        // Act
        var albums = _libraryService.AllAlbums;
        var artists = _libraryService.AllArtists;

        // Assert - Albums
        Assert.NotNull(albums);
        Assert.Equal(4, albums.Count);

        var a1_A = albums.FirstOrDefault(a => a.Title == "Album 1" && a.Artist == "Artist A");
        Assert.NotNull(a1_A);
        Assert.Equal(2, a1_A.SongCount);
        Assert.Equal("1.mp3", a1_A.RepresentativeFilePath); // Prioritizes art

        var a1_B = albums.FirstOrDefault(a => a.Title == "Album 1" && a.Artist == "Artist B");
        Assert.NotNull(a1_B);
        Assert.Equal(1, a1_B.SongCount);

        // Assert - Artists
        Assert.NotNull(artists);
        Assert.Equal(3, artists.Count);

        var artistA = artists.FirstOrDefault(a => a.Name == "Artist A");
        Assert.NotNull(artistA);
        Assert.Equal(3, artistA.SongCount);
        Assert.Equal(2, artistA.AlbumCount);
    }
}