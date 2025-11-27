using Sonorize.Core.Models;

namespace Sonorize.Core.Services.Audio;

public interface IPlayerService : IDisposable
{
    List<Song> PlaybackQueue { get; }
    int CurrentQueueIndex { get; }
    Song? CurrentSong { get; }
    bool IsPlaying { get; }
    bool IsSeeking { get; }
    TimeSpan SeekPreviewTime { get; }
    bool IsShuffle { get; }
    RepeatMode RepeatMode { get; }
    TimeSpan CurrentTime { get; }
    TimeSpan TotalTime { get; }
    float Volume { get; }

    // FX State
    float Tempo { get; }
    float Pitch { get; }

    bool IsAudioEngineAvailable { get; }
    bool IsFxAvailable { get; }

    event Action? PlaybackStateChanged;
    event Action<bool, RepeatMode>? PlaybackModesChanged;
    event Action? PlaybackProgressed;
    event Action<float>? VolumeChanged;
    event Action? QueueChanged;

    Task PlaySong(Song song, List<Song> songContext);
    Task PlayPlaylist(List<Song> songs, int startIndex = 0);
    Task TogglePlayback();
    void PlayNext();
    void PlayPrevious();
    void ToggleShuffle();
    void ToggleRepeat();
    void StartSeek();
    void EndSeek();
    void PreviewSeek(double percentage);
    void Seek(double percentage);
    void SetVolume(float volume);

    void RemoveFromQueue(int index);
    void ReorderQueue(int oldIndex, int newIndex);

    List<AudioDeviceInfo> GetAvailableAudioDevices();
    void ChangeOutputDevice(string? deviceName);

    // EQ
    void SetEq(bool enabled, float[] gains);
    void UpdateEqBand(int band, float gain);

    // FX
    void SetTempo(float percentage);
    void SetPitch(float semitones);
}