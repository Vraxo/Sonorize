using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Sonorize.Core.Services.UI;

namespace Sonorize.Photino.Blazor.Shared;

public partial class ThemeController
{
    private MarkupString _dynamicCss;
    private bool _hasCustomBg;
    private bool _useTransparency;

    protected override void OnInitialized()
    {
        SettingsManager.SettingsSaved += OnSettingsChanged;
        UpdateState();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await ApplyJsUpdates();
        }
    }

    private void OnSettingsChanged()
    {
        _ = InvokeAsync(async () =>
        {
            UpdateState();
            await ApplyJsUpdates();
            StateHasChanged();
        });
    }

    private void UpdateState()
    {
        string? bgPath = AppSettings.Theme.BackgroundImagePath;
        _hasCustomBg = !string.IsNullOrWhiteSpace(bgPath) && System.IO.File.Exists(bgPath);
        _useTransparency = _hasCustomBg || AppSettings.Theme.EnableAmbientBackground;

        string css = ThemeUtils.GenerateRootCss(AppSettings.Theme, _useTransparency);
        _dynamicCss = new MarkupString(css);
    }

    private async Task ApplyJsUpdates()
    {
        try
        {
            await JS.InvokeVoidAsync("updateThemeClasses",
                AppSettings.Theme.EnableZebraStriping,
                AppSettings.Theme.EnableCustomScrollbars,
                AppSettings.Theme.ShowGridLines);

            await JS.InvokeVoidAsync("applyCustomCss", AppSettings.Theme.CustomCss ?? "");
        }
        catch { }
    }

    public void Dispose()
    {
        SettingsManager.SettingsSaved -= OnSettingsChanged;
    }
}