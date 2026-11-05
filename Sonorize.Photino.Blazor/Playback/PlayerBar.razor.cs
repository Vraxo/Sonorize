using Sonorize.Core.Settings;

namespace Sonorize.Photino.Blazor.Playback;

public partial class PlayerBar
{
    protected override void OnInitialized()
    {
        SettingsManager.SettingsSaved += OnSettingsChanged;
    }

    private void OnSettingsChanged()
    {
        _ = InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        SettingsManager.SettingsSaved -= OnSettingsChanged;
    }
}
