using Sonorize.Core.Services.UI;
using Sonorize.Core.Settings;
using Sonorize.Photino.Blazor.Components.Pages.SettingsSections.Components;

namespace Sonorize.Photino.Blazor.Components.Pages.SettingsSections;

public partial class ThemeTab
{
    private ThemeListSection? _themeList;
    private bool _showSaveModal = false;

    private void ApplyLoadedTheme(SonorizeTheme theme)
    {
        AppSettings.ApplyTheme(theme);
        Save();
        StateHasChanged();
    }

    private void OpenSaveModal()
    {
        _showSaveModal = true;
    }

    private void CloseSaveModal()
    {
        _showSaveModal = false;
    }

    private void ConfirmSaveTheme(string name)
    {
        ThemeService.SaveTheme(name, AppSettings.ExtractTheme());
        _themeList?.RefreshThemes();
        CloseSaveModal();
    }

    private void Save()
    {
        SettingsManager.Save(AppSettings);
    }
}