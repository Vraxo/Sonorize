using Sonorize.Core.Models;
using Sonorize.Core.Services.Library;

namespace Sonorize.Core.Tests;

public class PlaylistPersistenceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly PlaylistPersistenceService _service;

    public PlaylistPersistenceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "Sonorize_PlaylistTests_" + Guid.NewGuid());
        _service = new PlaylistPersistenceService(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public void SavePlaylist_CreatesFileWithCorrectContent()
    {
        // Arrange
        var playlist = new Playlist
        {
            Id = Guid.NewGuid(),
            Name = "My Test Playlist",
            Type = PlaylistType.Manual,
            SongFilePaths = ["song1.mp3", "song2.mp3"]
        };

        // Act
        _service.SavePlaylist(playlist);

        // Assert
        string expectedPath = Path.Combine(_testDirectory, $"{playlist.Id}.json");
        Assert.True(File.Exists(expectedPath));

        string content = File.ReadAllText(expectedPath);
        Assert.Contains(playlist.Name, content);
        Assert.Contains("song1.mp3", content);
    }

    [Fact]
    public void LoadPlaylists_ReturnsSavedPlaylists()
    {
        // Arrange
        var p1 = new Playlist { Id = Guid.NewGuid(), Name = "A Playlist" };
        var p2 = new Playlist { Id = Guid.NewGuid(), Name = "B Playlist" };

        _service.SavePlaylist(p1);
        _service.SavePlaylist(p2);

        // Act
        var loaded = _service.LoadPlaylists();

        // Assert
        Assert.Equal(2, loaded.Count);
        Assert.Contains(loaded, p => p.Id == p1.Id && p.Name == p1.Name);
        Assert.Contains(loaded, p => p.Id == p2.Id && p.Name == p2.Name);
    }

    [Fact]
    public void DeletePlaylist_RemovesFile()
    {
        // Arrange
        var playlist = new Playlist { Id = Guid.NewGuid(), Name = "Delete Me" };
        _service.SavePlaylist(playlist);
        string path = Path.Combine(_testDirectory, $"{playlist.Id}.json");
        Assert.True(File.Exists(path)); // Pre-check

        // Act
        _service.DeletePlaylist(playlist);

        // Assert
        Assert.False(File.Exists(path));
    }
}