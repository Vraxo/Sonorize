using Sonorize.Core.Models;

namespace Sonorize.Core.Services.Audio;

public interface IAudioService : IDisposable
{
    event Action? PlaybackFinished;
    PlaybackState PlaybackState { get; }
    TimeSpan CurrentTime { get; set; }
    TimeSpan TotalTime { get; }
    float Volume { get; set; }

    bool IsAudioEngineAvailable { get; }
    bool IsFxAvailable { get; } // NEW: Check if bass_fx is loaded

    void Load(string filePath);
    void Play();
    void Pause();
    void Stop();

    List<AudioDeviceInfo> GetOutputDevices();
    void SetOutputDevice(string? deviceId);

    // EQ
    void SetEq(bool enabled, float[] gains);
    void UpdateEqBand(int bandIndex, float gain);

    // FX (Tempo/Pitch)
    void SetTempo(float percentage);
    void SetPitch(float semitones);
}