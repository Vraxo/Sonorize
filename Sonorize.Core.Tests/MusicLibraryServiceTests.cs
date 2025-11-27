using Sonorize.Core.Services.Library;

namespace Sonorize.Core.Tests;

public class MusicLibraryServiceTests : IDisposable
{
    private readonly string _tempTestDirectory;
    private readonly MusicLibraryService _musicLibraryService;
    private readonly List<string> _defaultExtensions = [".mp3", ".flac", ".wav", ".m4a"];

    public MusicLibraryServiceTests()
    {
        _tempTestDirectory = Path.Combine(Path.GetTempPath(), "SonorizeMusicLibTests_" + Guid.NewGuid());
        _ = Directory.CreateDirectory(_tempTestDirectory);
        _musicLibraryService = new MusicLibraryService();
    }

    public void Dispose()
    {
        if (!Directory.Exists(_tempTestDirectory))
        {
            return;
        }

        Directory.Delete(_tempTestDirectory, true);
    }

    private void CreateDummyFile(string path)
    {
        string fullPath = Path.Combine(_tempTestDirectory, path);
        _ = Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, "dummy content");
    }

    [Fact]
    public async Task LoadSongsFromFolderAsync_WhenDirectoryExists_FindsSupportedFiles()
    {
        // Arrange
        CreateDummyFile("song1.mp3");
        CreateDummyFile("song2.flac");
        CreateDummyFile("cover.jpg");
        CreateDummyFile("lyrics.txt");
        CreateDummyFile("audio.wav");

        // Act
        List<Models.Song> songs = await _musicLibraryService.LoadSongsFromFolderAsync(_tempTestDirectory, _defaultExtensions, CancellationToken.None);

        // Assert
        Assert.Equal(3, songs.Count);
        Assert.Contains(songs, s => s.FilePath.EndsWith("song1.mp3"));
        Assert.Contains(songs, s => s.FilePath.EndsWith("song2.flac"));
        Assert.Contains(songs, s => s.FilePath.EndsWith("audio.wav"));
        Assert.DoesNotContain(songs, s => s.FilePath.EndsWith("cover.jpg"));
    }

    [Fact]
    public async Task LoadSongsFromFolderAsync_SearchesSubdirectories()
    {
        // Arrange
        CreateDummyFile("rock/artist1/song.mp3");
        CreateDummyFile("pop/song.m4a");
        CreateDummyFile("data.txt");

        // Act
        List<Models.Song> songs = await _musicLibraryService.LoadSongsFromFolderAsync(_tempTestDirectory, _defaultExtensions, CancellationToken.None);

        // Assert
        Assert.Equal(2, songs.Count);
        Assert.Contains(songs, s => s.FilePath.EndsWith("song.mp3"));
        Assert.Contains(songs, s => s.FilePath.EndsWith("song.m4a"));
    }

    [Fact]
    public async Task LoadSongsFromFolderAsync_WhenDirectoryDoesNotExist_ReturnsEmptyList()
    {
        // Arrange
        string nonExistentPath = Path.Combine(_tempTestDirectory, "non_existent_folder");

        // Act
        List<Models.Song> songs = await _musicLibraryService.LoadSongsFromFolderAsync(nonExistentPath, _defaultExtensions, CancellationToken.None);

        // Assert
        Assert.NotNull(songs);
        Assert.Empty(songs);
    }

    [Fact]
    public async Task ProcessMusicFile_WhenTagLibFails_ReturnsSongWithFileNameAsTitle()
    {
        // Arrange
        string corruptFilePath = Path.Combine(_tempTestDirectory, "corrupt.mp3");
        // Must contain data to pass IsFileValid guard (length > 0), but be invalid MP3 to fail TagLib
        File.WriteAllText(corruptFilePath, "INVALID_BINARY_DATA_TO_TRIGGER_PARSER_ERROR");

        // Act
        List<Models.Song> songs = await _musicLibraryService.LoadSongsFromFolderAsync(
            _tempTestDirectory,
            _defaultExtensions,
            CancellationToken.None);

        // Assert
        _ = Assert.Single(songs);
        Models.Song song = songs[0];
        Assert.Equal("corrupt", song.Title);
        Assert.Equal("Unknown Artist", song.Artist);
        Assert.Equal(TimeSpan.Zero, song.Duration);
        Assert.Equal(corruptFilePath, song.FilePath);
    }
}