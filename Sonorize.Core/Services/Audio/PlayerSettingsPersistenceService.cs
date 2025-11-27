using Sonorize.Core.Helpers;
using Sonorize.Core.Settings;

namespace Sonorize.Core.Services.Audio;

public class PlayerSettingsPersistenceService : IDisposable
{
    private readonly SonorizeSettings _settings;
    private readonly ISettingsManager<SonorizeSettings> _settingsManager;
    private readonly ActionDebouncer _debouncer = new();

    public PlayerSettingsPersistenceService(
        IPlayerService playerService,
        SonorizeSettings settings,
        ISettingsManager<SonorizeSettings> settingsManager)
    {
        _settings = settings;
        _settingsManager = settingsManager;

        playerService.PlaybackModesChanged += OnPlaybackModesChanged;
        playerService.VolumeChanged += OnVolumeChanged;
    }

    private void OnPlaybackModesChanged(bool isShuffle, RepeatMode repeatMode)
    {
        _settings.Playback.IsShuffle = isShuffle;
        _settings.Playback.RepeatMode = repeatMode;
        DebounceSave();
    }

    private void OnVolumeChanged(float volume)
    {
        _settings.Playback.Volume = volume;
        DebounceSave();
    }

    private void DebounceSave()
    {
        _debouncer.Debounce(() =>
        {
            try
            {
                _settingsManager.Save(_settings);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Settings] Failed to save player settings: {ex.Message}");
            }
        }, 500);
    }

    public void Dispose()
    {
        _debouncer.Dispose();
    }
}