using Sonorize.Core.Models;

namespace Sonorize.Core.Services.Audio;

public class PlayerService : IPlayerService
{
    private readonly IAudioService _audio;
    private readonly QueueController _queue;

    // Fix: Use global:: alias to avoid collision if a local 'System' namespace exists
    private readonly global::System.Timers.Timer _playbackTimer;

    // State
    public List<Song> PlaybackQueue => _queue.Queue;
    public int CurrentQueueIndex => _queue.CurrentIndex;
    public Song? CurrentSong => _queue.CurrentSong;

    public bool IsShuffle => _queue.IsShuffle;
    public RepeatMode RepeatMode => _queue.RepeatMode;

    public bool IsPlaying => _audio.PlaybackState == PlaybackState.Playing;
    public bool IsSeeking { get; private set; } = false;
    public TimeSpan SeekPreviewTime { get; private set; } = TimeSpan.Zero;

    // FX
    public float Tempo { get; private set; }
    public float Pitch { get; private set; }
    public bool IsAudioEngineAvailable => _audio.IsAudioEngineAvailable;
    public bool IsFxAvailable => _audio.IsFxAvailable;

    public TimeSpan CurrentTime => _audio.CurrentTime;
    public TimeSpan TotalTime => _audio.TotalTime;
    public float Volume => _audio.Volume;

    // Events
    public event Action? PlaybackStateChanged;
    public event Action<bool, RepeatMode>? PlaybackModesChanged;
    public event Action? PlaybackProgressed;
    public event Action<float>? VolumeChanged;
    public event Action? QueueChanged;

    public PlayerService(
        IAudioService audio,
        QueueController queue,
        bool initialIsShuffle,
        RepeatMode initialRepeatMode,
        float initialVolume,
        string? initialDeviceName,
        float initialTempo,
        float initialPitch)
    {
        _audio = audio;
        _queue = queue;

        _audio.PlaybackFinished += OnPlaybackFinished;
        _queue.QueueChanged += () => QueueChanged?.Invoke();
        _queue.ModesChanged += (s, r) => PlaybackModesChanged?.Invoke(s, r);

        _queue.SetModes(initialIsShuffle, initialRepeatMode);

        Tempo = initialTempo;
        Pitch = initialPitch;

        _audio.SetOutputDevice(initialDeviceName);
        _audio.Volume = initialVolume;
        _audio.SetTempo(Tempo);
        _audio.SetPitch(Pitch);

        // Increased resolution for smoother UI (100ms = 10 FPS)
        _playbackTimer = new global::System.Timers.Timer(100);
        _playbackTimer.Elapsed += (s, e) => UpdatePlaybackProgress();
        _playbackTimer.AutoReset = true;
    }

    // --- Core Playback ---

    public Task PlaySong(Song song, List<Song> songContext)
    {
        if (!IsAudioEngineAvailable)
        {
            return Task.CompletedTask;
        }

        // Update context if it's a new list, otherwise just jump index
        // Simple comparison: if counts differ or first item differs, assume new context
        bool isNewContext = _queue.Queue != songContext;

        if (isNewContext)
        {
            _queue.SetContext(songContext);
        }

        _queue.SetIndexBySong(song);
        PlayCurrentSong();
        return Task.CompletedTask;
    }

    public Task PlayPlaylist(List<Song> songs, int startIndex = 0)
    {
        if (!IsAudioEngineAvailable || songs.Count == 0)
        {
            return Task.CompletedTask;
        }

        _queue.SetContext(songs);
        _queue.SetIndex(startIndex);
        PlayCurrentSong();
        return Task.CompletedTask;
    }

    public Task TogglePlayback()
    {
        if (!IsAudioEngineAvailable)
        {
            return Task.CompletedTask;
        }

        if (CurrentSong is null && _queue.Queue.Any())
        {
            _queue.SetIndex(0); // Default to start
            PlayCurrentSong();
        }
        else if (CurrentSong is not null)
        {
            if (IsPlaying)
            {
                _audio.Pause();
                _playbackTimer.Stop();
            }
            else
            {
                _audio.Play();
                _playbackTimer.Start();
            }
            PlaybackStateChanged?.Invoke();
        }

        return Task.CompletedTask;
    }

    private void PlayCurrentSong()
    {
        var song = CurrentSong;
        if (song is null)
        {
            return;
        }

        _audio.Load(song.FilePath);
        _audio.Play();
        _playbackTimer.Start();

        PlaybackStateChanged?.Invoke();
    }

    private void OnPlaybackFinished()
    {
        // BASS callback happens on a mixer thread. We must dispatch to Task pool 
        // to avoid deadlocks when calling BASS functions that might wait for the mixer.
        _ = Task.Run(() =>
        {
            if (RepeatMode == RepeatMode.One)
            {
                PlayCurrentSong();
                return;
            }

            if (_queue.TryAdvance(autoAdvance: true))
            {
                PlayCurrentSong();
            }
            else
            {
                StopPlayback();
            }
        });
    }

    public void PlayNext()
    {
        if (!IsAudioEngineAvailable || !_queue.Queue.Any())
        {
            return;
        }

        if (_queue.TryAdvance(autoAdvance: false))
        {
            PlayCurrentSong();
        }
    }

    public void PlayPrevious()
    {
        if (!IsAudioEngineAvailable || !_queue.Queue.Any())
        {
            return;
        }

        if (_audio.CurrentTime.TotalSeconds > 3)
        {
            _audio.CurrentTime = TimeSpan.Zero;
            PlaybackProgressed?.Invoke();
            return;
        }

        if (_queue.TryRegress())
        {
            PlayCurrentSong();
        }
    }

    // --- Mode Passthrough ---

    public void ToggleShuffle()
    {
        _queue.ToggleShuffle();
    }

    public void ToggleRepeat()
    {
        _queue.ToggleRepeat();
    }

    // --- Queue Management Passthrough ---

    public void RemoveFromQueue(int index)
    {
        _queue.Remove(index);
    }

    public void ReorderQueue(int oldIndex, int newIndex)
    {
        _queue.Reorder(oldIndex, newIndex);
    }

    // --- Standard Methods ---

    private void StopPlayback()
    {
        _audio.Stop();
        _playbackTimer.Stop();
        PlaybackStateChanged?.Invoke();
    }

    public void SetEq(bool enabled, float[] gains)
    {
        _audio.SetEq(enabled, gains);
    }

    public void UpdateEqBand(int band, float gain)
    {
        _audio.UpdateEqBand(band, gain);
    }

    public void SetTempo(float percentage)
    {
        Tempo = percentage;
        _audio.SetTempo(percentage);
        PlaybackStateChanged?.Invoke();
    }

    public void SetPitch(float semitones)
    {
        Pitch = semitones;
        _audio.SetPitch(semitones);
        PlaybackStateChanged?.Invoke();
    }

    public List<AudioDeviceInfo> GetAvailableAudioDevices()
    {
        return _audio.GetOutputDevices();
    }

    public void ChangeOutputDevice(string? deviceName)
    {
        _audio.SetOutputDevice(deviceName);
    }

    public void StartSeek() { IsSeeking = true; }
    public void EndSeek() { IsSeeking = false; }

    public void PreviewSeek(double percentage)
    {
        if (TotalTime > TimeSpan.Zero)
        {
            SeekPreviewTime = TimeSpan.FromSeconds(TotalTime.TotalSeconds * percentage);
            PlaybackProgressed?.Invoke();
        }
    }

    public void Seek(double percentage)
    {
        if (TotalTime > TimeSpan.Zero)
        {
            _audio.CurrentTime = TimeSpan.FromSeconds(TotalTime.TotalSeconds * percentage);
            PlaybackProgressed?.Invoke();
        }
    }

    public void SetVolume(float volume)
    {
        volume = float.Clamp(volume, 0.0f, 1.0f);
        _audio.Volume = volume;
        VolumeChanged?.Invoke(volume);
    }

    private void UpdatePlaybackProgress()
    {
        if (IsPlaying && !IsSeeking)
        {
            PlaybackProgressed?.Invoke();
        }
    }

    public void Dispose()
    {
        _audio.PlaybackFinished -= OnPlaybackFinished;
        _playbackTimer.Dispose();
        GC.SuppressFinalize(this);
    }
}