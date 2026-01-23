using Sonorize.Core.Helpers;
using Sonorize.Core.Models;
using Sonorize.Core.Services.Library;

namespace Sonorize.Photino.Blazor.Components.Pages.SettingsSections;

public partial class AdvancedTab
{
    private readonly ActionDebouncer _debouncer = new();

    private string CustomCss
    {
        get => AppSettings.Theme.CustomCss;

        set
        {
            if (AppSettings.Theme.CustomCss == value)
            {
                return;
            }

            AppSettings.Theme.CustomCss = value;
            DebounceSave();
        }
    }

    private void DebounceSave()
    {
        _debouncer.Debounce(() =>
        {
            try
            {
                SettingsManager.Save(AppSettings);
            }
            catch { }
        }, 500);
    }

    private void Save()
    {
        SettingsManager.Save(AppSettings);
    }

    private void LoadDemoData()
    {
        LibraryService.LoadDemoData();
    }

    private void SimulateUpdate()
    {
        // 1. Generate Mock Data (Test Responsibility)
        ReleaseInfo mockRelease = MockUpdateGenerator.Generate();

        // 2. Inject into Service (Service Responsibility)
        UpdateService.SimulateUpdate(mockRelease);
    }

    public void Dispose()
    {
        _debouncer.Dispose();
    }
}