namespace Sonorize.Photino.Blazor.Shared.Player;

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