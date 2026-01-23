using Microsoft.AspNetCore.Components;
using Sonorize.Core.Models;

namespace Sonorize.Photino.Blazor.Shared.Modals;

public partial class EqualizerModal
{
    [Parameter] public bool IsVisible { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }

    private string _currentPresetName = "";
    private List<EqPreset> _cachedPresets = [];

    private IEnumerable<EqPreset> UserPresets => _cachedPresets.Where(p => !p.IsDefault);
    private IEnumerable<EqPreset> DefaultPresets => _cachedPresets.Where(p => p.IsDefault);

    private bool CanDelete
    {
        get
        {
            if (string.IsNullOrEmpty(_currentPresetName) || _currentPresetName == "__custom__")
            {
                return false;
            }

            EqPreset? preset = _cachedPresets.FirstOrDefault(p => p.Name == _currentPresetName);
            return preset is not null && !preset.IsDefault;
        }
    }

    protected override void OnInitialized()
    {
        ReloadPresets();
    }

    private void ReloadPresets()
    {
        _cachedPresets = PresetService.LoadPresets();
    }

    private async Task Close()
    {
        await OnClose.InvokeAsync();
    }

    // --- Actions ---

    private void OnEnabledChanged(bool enabled)
    {
        AppSettings.Playback.EqEnabled = enabled;
        PlayerService.SetEq(enabled, AppSettings.Playback.EqGains);
        SettingsManager.Save(AppSettings);
    }

    private void OnPresetSelected(string name)
    {
        if (string.IsNullOrEmpty(name) || name == "__custom__")
        {
            return;
        }

        EqPreset? preset = _cachedPresets.FirstOrDefault(p => p.Name == name);

        if (preset is null)
        {
            return;
        }

        ApplyPreset(preset);
    }

    private void ApplyPreset(EqPreset preset)
    {
        Array.Copy(preset.Gains, AppSettings.Playback.EqGains, 10);
        PlayerService.SetEq(AppSettings.Playback.EqEnabled, AppSettings.Playback.EqGains);

        _currentPresetName = preset.Name;

        SettingsManager.Save(AppSettings);
        StateHasChanged();
    }

    private void OnGainChanged((int Index, float Value) args)
    {
        AppSettings.Playback.EqGains[args.Index] = args.Value;
        PlayerService.UpdateEqBand(args.Index, args.Value);
        _currentPresetName = "__custom__";
        SettingsManager.Save(AppSettings);
    }

    private void OnSavePreset(string name)
    {
        EqPreset? existing = _cachedPresets.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        // Recursively handle default collisions
        if (existing is not null && existing.IsDefault)
        {
            OnSavePreset(name + " 1");
            return;
        }

        EqPreset presetToSave = existing
            ?? new()
            {
                Name = name,
                IsDefault = false,
                Gains = new float[10]
            };

        // Update content
        Array.Copy(AppSettings.Playback.EqGains, presetToSave.Gains, 10);

        if (existing is null)
        {
            _cachedPresets.Add(presetToSave);
        }

        PresetService.SavePreset(presetToSave);
        ReloadPresets(); // Refresh from disk/source

        _currentPresetName = presetToSave.Name;
        SettingsManager.Save(AppSettings);
    }

    private void OnDeletePreset()
    {
        EqPreset? preset = _cachedPresets.FirstOrDefault(p => p.Name == _currentPresetName);

        if (preset is null || preset.IsDefault)
        {
            return;
        }

        PresetService.DeletePreset(preset);
        ReloadPresets();
        _currentPresetName = "__custom__";
        SettingsManager.Save(AppSettings);
    }
}