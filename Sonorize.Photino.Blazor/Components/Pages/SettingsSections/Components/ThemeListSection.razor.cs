using Microsoft.AspNetCore.Components;
using Sonorize.Core.Services.UI;
using Sonorize.Core.Settings;

namespace Sonorize.Photino.Blazor.Components.Pages.SettingsSections.Components;

public partial class ThemeListSection
{
    [Parameter] public EventCallback<SonorizeTheme> OnLoadTheme { get; set; }

    private List<string> _availableThemes = [];
    private string _selectedTheme = "";

    protected override void OnInitialized()
    {
        RefreshThemes();
    }

    public void RefreshThemes()
    {
        _availableThemes = ThemeService.GetAvailableThemes();

        if (!string.IsNullOrEmpty(_selectedTheme) && !_availableThemes.Contains(_selectedTheme))
        {
            _selectedTheme = "";
        }

        StateHasChanged();
    }

    private async Task OnSelectionChanged(ChangeEventArgs e)
    {
        _selectedTheme = e.Value?.ToString() ?? "";

        if (string.IsNullOrEmpty(_selectedTheme))
        {
            return;
        }

        SonorizeTheme? theme = ThemeService.LoadTheme(_selectedTheme);

        if (theme is null)
        {
            return;
        }

        await OnLoadTheme.InvokeAsync(theme);
    }

    private void DeleteSelected()
    {
        if (string.IsNullOrEmpty(_selectedTheme))
        {
            return;
        }

        ThemeService.DeleteTheme(_selectedTheme);
        RefreshThemes();
    }
}