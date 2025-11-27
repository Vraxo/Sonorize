using ManagedBass;
using ManagedBass.Fx; // Required for Tempo/Pitch
using Sonorize.Core.Interop;
using Sonorize.Core.Models;
using Sonorize.Core.Services.System;

namespace Sonorize.Core.Services.Audio;

public class AudioService : IAudioService
{
    private readonly LogService _logger;
    private int _streamHandle;
    private int _sourceHandle; // Required for FX streams (this is the decode stream)
    private bool _isManuallyStopping = false;
    private string? _preferredDeviceIndexString;

    // EQ State
    private readonly int[] _eqHandles = new int[10];
    private readonly float[] _eqGains = new float[10];
    private bool _eqEnabled = false;
    private readonly float[] _eqCenters = [31, 63, 125, 250, 500, 1000, 2000, 4000, 8000, 16000];

    // FX State
    private float _currentTempo = 0;
    private float _currentPitch = 0;

    public bool IsAudioEngineAvailable { get; private set; } = false;
    public bool IsFxAvailable { get; private set; } = false;

    public event Action? PlaybackFinished;

    public Models.PlaybackState PlaybackState => !IsAudioEngineAvailable || _streamHandle == 0
                ? Models.PlaybackState.Stopped
                : Bass.ChannelIsActive(_streamHandle) switch
                {
                    ManagedBass.PlaybackState.Playing => Sonorize.Core.Models.PlaybackState.Playing,
                    ManagedBass.PlaybackState.Paused => Sonorize.Core.Models.PlaybackState.Paused,
                    _ => Sonorize.Core.Models.PlaybackState.Stopped
                };

    public TimeSpan CurrentTime
    {
        get
        {
            if (!IsAudioEngineAvailable || _streamHandle == 0)
            {
                return TimeSpan.Zero;
            }

            long pos = Bass.ChannelGetPosition(_streamHandle);
            double secs = Bass.ChannelBytes2Seconds(_streamHandle, pos);
            return TimeSpan.FromSeconds(secs);
        }
        set
        {
            if (IsAudioEngineAvailable && _streamHandle != 0)
            {
                long pos = Bass.ChannelSeconds2Bytes(_streamHandle, value.TotalSeconds);
                _ = Bass.ChannelSetPosition(_streamHandle, pos);
            }
        }
    }

    public TimeSpan TotalTime
    {
        get
        {
            if (!IsAudioEngineAvailable || _streamHandle == 0)
            {
                return TimeSpan.Zero;
            }

            // For Tempo streams, length is based on the source
            long len = Bass.ChannelGetLength(_streamHandle);
            double secs = Bass.ChannelBytes2Seconds(_streamHandle, len);
            return TimeSpan.FromSeconds(secs);
        }
    }

    public float Volume
    {
        get;
        set
        {
            field = Math.Clamp(value, 0.0f, 1.0f);
            if (IsAudioEngineAvailable && _streamHandle != 0)
            {
                _ = Bass.ChannelSetAttribute(_streamHandle, ChannelAttribute.Volume, field);
            }
        }
    } = 1.0f;

    public AudioService(LogService logger)
    {
        _logger = logger;
        InitializeBass();
    }

    private void InitializeBass()
    {
        try
        {
            if (Bass.Init(-1, 44100, DeviceInitFlags.Default, IntPtr.Zero))
            {
                IsAudioEngineAvailable = true;
            }
            else if (Bass.LastError == Errors.Already)
            {
                IsAudioEngineAvailable = true;
            }

            if (!IsAudioEngineAvailable)
            {
                _logger.Error($"BASS Init Failed: {Bass.LastError}");
            }

            // Check for BASS_FX
            try
            {
                // Just accessing this property checks if the DLL is loaded/accessible
                Version fxVersion = BassFx.Version;
                IsFxAvailable = fxVersion != null;
            }
            catch
            {
                IsFxAvailable = false;
                _logger.Warn("bass_fx library not found. Speed/Pitch controls disabled.");
            }
        }
        catch (Exception ex)
        {
            IsAudioEngineAvailable = false;
            _logger.Error("Critical BASS initialization failure", ex);
        }
    }

    public List<AudioDeviceInfo> GetOutputDevices()
    {
        List<AudioDeviceInfo> devices = [];
        if (!IsAudioEngineAvailable)
        {
            devices.Add(new AudioDeviceInfo { Index = -1, Name = "Audio Engine Missing", IsDefault = true });
            return devices;
        }

        try
        {
            int count = Bass.DeviceCount;
            for (int i = 0; i < count; i++)
            {
                if (Bass.GetDeviceInfo(i, out DeviceInfo info))
                {
                    if (info.IsEnabled && info.Type != DeviceType.Microphone)
                    {
                        devices.Add(new AudioDeviceInfo { Index = i, Name = info.Name, IsDefault = info.IsDefault });
                    }
                }
            }
        }
        catch { }
        return devices;
    }

    public void SetOutputDevice(string? deviceIndexString)
    {
        if (!IsAudioEngineAvailable)
        {
            return;
        }

        if (_preferredDeviceIndexString == deviceIndexString)
        {
            return;
        }

        _preferredDeviceIndexString = deviceIndexString;

        if (int.TryParse(deviceIndexString, out int deviceIndex))
        {
            string? currentFile = GetCurrentFile();
            TimeSpan currentPos = CurrentTime;
            bool wasPlaying = PlaybackState == Sonorize.Core.Models.PlaybackState.Playing;

            Stop();
            _ = Bass.Free();

            if (Bass.Init(deviceIndex, 44100, DeviceInitFlags.Default, IntPtr.Zero))
            {
                if (currentFile != null)
                {
                    Load(currentFile);
                    CurrentTime = currentPos;
                    if (wasPlaying)
                    {
                        Play();
                    }
                }
            }
            else
            {
                _logger.Error($"Failed to switch audio device: {Bass.LastError}");
            }
        }
    }

    private string? _currentFilePath;
    private string? GetCurrentFile()
    {
        return _currentFilePath;
    }

    public void Load(string filePath)
    {
        if (!IsAudioEngineAvailable)
        {
            return;
        }

        Stop();

        // Guard against demo data playback
        if (filePath.StartsWith("demo://", StringComparison.OrdinalIgnoreCase))
        {
            _logger.Info($"Ignored playback request for demo track: {filePath}");
            return;
        }

        if (Bass.DeviceCount > 0 && Bass.CurrentDevice == -1)
        {
            _ = Bass.Init(-1, 44100, DeviceInitFlags.Default, IntPtr.Zero);
        }

        int stream = 0;

        // Try to create FX Tempo stream first
        if (IsFxAvailable)
        {
            // Create DECODE stream (source for FX)
            int source = Bass.CreateStream(filePath, 0, 0, BassFlags.Decode | BassFlags.Prescan | BassFlags.Float);

            if (source != 0)
            {
                // Create TEMPO stream
                // BassFlags.FxFreeSource ensures the source is freed when tempo stream is freed
                stream = BassFx.TempoCreate(source, BassFlags.FxFreeSource);

                if (stream != 0)
                {
                    _sourceHandle = source;
                }
                else
                {
                    // Fallback if Tempo creation failed but source worked (unlikely, but safe)
                    _ = Bass.StreamFree(source);
                }
            }
        }

        // Fallback to standard stream if FX missing or failed
        if (stream == 0)
        {
            stream = Bass.CreateStream(filePath, 0, 0, BassFlags.Prescan | BassFlags.Float);
        }

        if (stream != 0)
        {
            _streamHandle = stream;
            _currentFilePath = filePath;

            // Apply Attributes
            _ = Bass.ChannelSetAttribute(_streamHandle, ChannelAttribute.Volume, Volume);

            // Re-apply FX settings if valid stream
            if (IsFxAvailable && _sourceHandle != 0)
            {
                ApplyTempoPitch();
            }

            if (_eqEnabled)
            {
                ApplyEqToStream();
            }

            // Setup Sync
            _ = Bass.ChannelSetSync(_streamHandle, SyncFlags.End, 0, (h, c, d, u) =>
            {
                _ = Task.Run(() =>
                {
                    if (!_isManuallyStopping)
                    {
                        PlaybackFinished?.Invoke();
                    }
                });
            }, IntPtr.Zero);
        }
        else
        {
            _logger.Error($"Failed to load stream: {filePath}. Error: {Bass.LastError}");
        }
    }

    public void Play()
    {
        if (IsAudioEngineAvailable && _streamHandle != 0)
        {
            if (!Bass.ChannelPlay(_streamHandle))
            {
                _logger.Error($"Play failed: {Bass.LastError}");
            }
        }
    }

    public void Pause()
    {
        if (IsAudioEngineAvailable && _streamHandle != 0)
        {
            _ = Bass.ChannelPause(_streamHandle);
        }
    }

    public void Stop()
    {
        if (IsAudioEngineAvailable && _streamHandle != 0)
        {
            _isManuallyStopping = true;
            _ = Bass.ChannelStop(_streamHandle);
            _ = Bass.StreamFree(_streamHandle);
            // _sourceHandle is freed automatically due to FxFreeSource flag
            _streamHandle = 0;
            _sourceHandle = 0;
            Array.Clear(_eqHandles, 0, _eqHandles.Length);
            _isManuallyStopping = false;
        }
    }

    // --- FX Logic ---

    public void SetTempo(float percentage)
    {
        _currentTempo = percentage;
        if (IsFxAvailable && _streamHandle != 0)
        {
            _ = Bass.ChannelSetAttribute(_streamHandle, ChannelAttribute.Tempo, _currentTempo);
        }
    }

    public void SetPitch(float semitones)
    {
        _currentPitch = semitones;
        if (IsFxAvailable && _streamHandle != 0)
        {
            _ = Bass.ChannelSetAttribute(_streamHandle, ChannelAttribute.Pitch, _currentPitch);
        }
    }

    private void ApplyTempoPitch()
    {
        if (!IsFxAvailable || _streamHandle == 0)
        {
            return;
        }

        _ = Bass.ChannelSetAttribute(_streamHandle, ChannelAttribute.Tempo, _currentTempo);
        _ = Bass.ChannelSetAttribute(_streamHandle, ChannelAttribute.Pitch, _currentPitch);
    }

    // --- EQ Logic ---

    public void SetEq(bool enabled, float[] gains)
    {
        _eqEnabled = enabled;
        if (gains.Length == 10)
        {
            Array.Copy(gains, _eqGains, 10);
        }

        if (_streamHandle != 0)
        {
            if (_eqEnabled)
            {
                if (_eqHandles[0] == 0)
                {
                    ApplyEqToStream();
                }
                else
                {
                    UpdateAllBands();
                }
            }
            else
            {
                RemoveEq();
            }
        }
    }

    public void UpdateEqBand(int bandIndex, float gain)
    {
        if (bandIndex is < 0 or >= 10)
        {
            return;
        }

        _eqGains[bandIndex] = gain;

        if (_streamHandle != 0 && _eqEnabled && _eqHandles[bandIndex] != 0)
        {
            UpdateBandParam(bandIndex);
        }
    }

    private void ApplyEqToStream()
    {
        if (_streamHandle == 0)
        {
            return;
        }

        for (int i = 0; i < 10; i++)
        {
            int fx = Bass.ChannelSetFX(_streamHandle, EffectType.DXParamEQ, 0);
            _eqHandles[i] = fx;
            UpdateBandParam(i);
        }
    }

    private void RemoveEq()
    {
        if (_streamHandle == 0)
        {
            return;
        }

        for (int i = 0; i < 10; i++)
        {
            if (_eqHandles[i] != 0)
            {
                _ = Bass.ChannelRemoveFX(_streamHandle, _eqHandles[i]);
                _eqHandles[i] = 0;
            }
        }
    }

    private void UpdateAllBands()
    {
        for (int i = 0; i < 10; i++)
        {
            UpdateBandParam(i);
        }
    }

    private void UpdateBandParam(int index)
    {
        if (_eqHandles[index] == 0)
        {
            return;
        }

        var param = new DXParamEQParams
        {
            fCenter = _eqCenters[index],
            fBandwidth = 12f,
            fGain = _eqGains[index]
        };

        if (!Bass.FXSetParameters(_eqHandles[index], param))
        {
            // EQ param update failures are rare but good to know about
            // _logger.Warn($"EQ update failed for band {index}");
        }
    }

    public void Dispose()
    {
        Stop();
        if (IsAudioEngineAvailable)
        {
            try
            {
                // Safety: Ensure we only free if we initialized it, and handle any random
                // thread-race exceptions during app shutdown gracefully.
                _ = Bass.Free();
            }
            catch { /* Swallowing shutdown errors is safer than crashing the app closing */ }
        }
        GC.SuppressFinalize(this);
    }
}