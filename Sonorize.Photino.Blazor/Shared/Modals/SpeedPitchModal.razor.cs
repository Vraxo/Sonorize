using Microsoft.AspNetCore.Components;
using System.Globalization;

namespace Sonorize.Photino.Blazor.Shared.Modals;

public partial class SpeedPitchModal
{
    [Parameter] public bool IsVisible { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }

    // Logic:
    // UI Range: 0 to 200 (Center 100)
    // BASS Range: -100 to +100 (Center 0)
    // Mapping: Bass = UI - 100
    // UI = Bass + 100

    private float UiTempo => AppSettings.Playback.Tempo + 100;
    private float PitchValue => AppSettings.Playback.Pitch;

    private void OnTempoInput(ChangeEventArgs e)
    {
        if (!float.TryParse(e.Value?.ToString(), out float val))
        {
            return;
        }
        float bassValue = val - 100;
        // BASS limit clamp (BASS usually supports -95 as min)
        if (bassValue < -95)
        {
            bassValue = -95;
        }

        AppSettings.Playback.Tempo = bassValue;
        PlayerService.SetTempo(bassValue);
        SettingsManager.Save(AppSettings);
    }

    private void OnPitchInput(ChangeEventArgs e)
    {
        if (!float.TryParse(e.Value?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out float val))
        {
            return;
        }

        AppSettings.Playback.Pitch = val;
        PlayerService.SetPitch(val);
        SettingsManager.Save(AppSettings);
    }

    private void ResetTempo()
    {
        AppSettings.Playback.Tempo = 0; // 0 internally = 100% in UI
        PlayerService.SetTempo(0);
        SettingsManager.Save(AppSettings);
    }

    private void ResetPitch()
    {
        AppSettings.Playback.Pitch = 0;
        PlayerService.SetPitch(0);
        SettingsManager.Save(AppSettings);
    }

    private string GetTempoString()
    {
        float factor = UiTempo / 100.0f;
        return $"{factor:0.00}x";
    }

    private async Task Close()
    {
        await OnClose.InvokeAsync();
    }
}