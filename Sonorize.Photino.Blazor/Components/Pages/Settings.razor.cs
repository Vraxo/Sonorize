namespace Sonorize.Photino.Blazor.Components.Pages;

public partial class Settings
{
    private enum SettingsTab { General, Theme, Library, Scrobbling, Advanced }
    private SettingsTab _activeTab = SettingsTab.General;

    private void GoBack()
    {
        Nav.NavigateTo("/");
    }
}