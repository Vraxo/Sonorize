using NSubstitute;
using Sonorize.Core.Models;
using Sonorize.Core.Services.Audio;

namespace Sonorize.Core.Tests;

public class PlayerServiceTests
{
    private readonly IAudioService _audioService = Substitute.For<IAudioService>();
    private readonly QueueController _queueController = new();

    private readonly List<Song> _songList;

    public PlayerServiceTests()
    {
        // Initialize songs with safe paths in constructor
        string root = Path.Combine(Environment.CurrentDirectory, "music");
        _songList =
        [
            new() { FilePath = Path.Combine(root, "song1.mp3"), Title = "Song 1" },
            new() { FilePath = Path.Combine(root, "song2.mp3"), Title = "Song 2" },
            new() { FilePath = Path.Combine(root, "song3.mp3"), Title = "Song 3" }
        ];
    }

    private PlayerService CreatePlayerService(
        bool isShuffle = false,
        RepeatMode repeatMode = RepeatMode.None,
        float volume = 1.0f,
        string? deviceName = null)
    {
        _ = _audioService.IsAudioEngineAvailable.Returns(true);

        return new PlayerService(
            _audioService,
            _queueController,
            isShuffle,
            repeatMode,
            volume,
            deviceName,
            0f, 0f);
    }

    [Fact]
    public void PlaySong_WhenCalled_LoadsAndPlaysCorrectSong()
    {
        // Arrange
        PlayerService player = CreatePlayerService();
        Song songToPlay = _songList[1];

        // Act
        _ = player.PlaySong(songToPlay, _songList);

        // Assert
        Assert.Equal(1, player.CurrentQueueIndex);
        Assert.Equal(songToPlay, player.CurrentSong);
        _audioService.Received(1).Load(songToPlay.FilePath);
        _audioService.Received(1).Play();
    }

    [Fact]
    public void PlayNext_WhenNotShuffledAndNotAtEnd_PlaysNextSong()
    {
        // Arrange
        PlayerService player = CreatePlayerService();
        _ = player.PlaySong(_songList[0], _songList);

        // Act
        player.PlayNext();

        // Assert
        Assert.Equal(1, player.CurrentQueueIndex);
        _audioService.Received(1).Load(_songList[1].FilePath);
    }

    [Fact]
    public void PlayNext_WhenAtEndAndRepeatAll_PlaysFirstSong()
    {
        // Arrange
        PlayerService player = CreatePlayerService(repeatMode: RepeatMode.All);
        _ = player.PlaySong(_songList[2], _songList);

        // Act
        player.PlayNext();

        // Assert
        Assert.Equal(0, player.CurrentQueueIndex);
        _audioService.Received(1).Load(_songList[0].FilePath);
    }

    [Fact]
    public void ToggleShuffle_WhenCalled_TogglesShuffleState()
    {
        // Arrange
        PlayerService player = CreatePlayerService(isShuffle: false);

        // Act
        player.ToggleShuffle();

        // Assert
        Assert.True(player.IsShuffle);
    }

    [Fact]
    public void ToggleRepeat_WhenCalled_CyclesState()
    {
        // Arrange
        PlayerService player = CreatePlayerService(repeatMode: RepeatMode.None);

        // Act & Assert
        player.ToggleRepeat();
        Assert.Equal(RepeatMode.All, player.RepeatMode);

        player.ToggleRepeat();
        Assert.Equal(RepeatMode.One, player.RepeatMode);

        player.ToggleRepeat();
        Assert.Equal(RepeatMode.None, player.RepeatMode);
    }
}