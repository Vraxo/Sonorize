using Microsoft.AspNetCore.Components;
using NativeFileDialogs.Net;
using Sonorize.Core.Settings;
using Sonorize.Core.Theming;

namespace Sonorize.Photino.Blazor.Theming;

public partial class ThemeListSection
{
    [Parameter] public EventCallback<SonorizeTheme> OnLoadTheme { get; set; }

    private string? _exportError;

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

    private void ExportSelected()
    {
        if (string.IsNullOrEmpty(_selectedTheme))
        {
            return;
        }

        Dictionary<string, string> filters = new()
        {
            { "Sonorize Theme", "sonorize-theme" }
        };

        string defaultName = $"{_selectedTheme}{ThemePackageArchive.FileExtension}";
        if (Nfd.SaveDialog(out string? path, filters, defaultName) != NfdStatus.Ok
            || string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            using FileStream destination = new(
                path,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None);

            ThemeService.ExportThemePackage(_selectedTheme, destination);
            _exportError = null;
        }
        catch (Exception exception)
        {
            _exportError = $"Theme export failed: {exception.Message}";
        }
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
