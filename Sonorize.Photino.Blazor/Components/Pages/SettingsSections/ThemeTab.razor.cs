using Microsoft.AspNetCore.Components;
using Sonorize.Core.Services.UI;
using Sonorize.Core.Settings;
using Sonorize.Photino.Blazor.Components.Pages.SettingsSections.Components;

namespace Sonorize.Photino.Blazor.Components.Pages.SettingsSections;

public partial class ThemeTab
{
    private ThemeListSection? _themeList;
    private bool _showSaveModal = false;
    private bool _showSaveColorModal = false;
    private bool _showSaveLayoutModal = false;

    private List<string> _colorThemes = new();
    private List<string> _layoutThemes = new();
    private string _selectedColorTheme = "";
    private string _selectedLayoutTheme = "";

    protected override void OnInitialized()
    {
        RefreshThemeLists();
        _selectedColorTheme = _colorThemes.FirstOrDefault() ?? "";
        _selectedLayoutTheme = _layoutThemes.FirstOrDefault() ?? "";
    }

    private void RefreshThemeLists()
    {
        _colorThemes = ThemeService.GetAvailableColorThemes();
        _layoutThemes = ThemeService.GetAvailableLayoutThemes();
    }

    private async Task OnColorThemeSelected(string name)
    {
        if (string.IsNullOrEmpty(name)) return;
        _selectedColorTheme = name;

        var colors = ThemeService.LoadThemeColors(name);
        if (colors != null)
        {
            var currentTheme = AppSettings.ExtractTheme();
            currentTheme.AccentColor = colors.AccentColor;
            currentTheme.BgPrimary = colors.BgPrimary;
            currentTheme.BgSecondary = colors.BgSecondary;
            currentTheme.BgTertiary = colors.BgTertiary;
            currentTheme.PlayerBarBg = colors.PlayerBarBg;
            currentTheme.BorderColor = colors.BorderColor;
            currentTheme.TextPrimary = colors.TextPrimary;
            currentTheme.TextSecondary = colors.TextSecondary;
            AppSettings.ApplyTheme(currentTheme);
            Save();
            StateHasChanged();
        }
    }

    private async Task OnLayoutThemeSelected(string name)
    {
        if (string.IsNullOrEmpty(name)) return;
        _selectedLayoutTheme = name;

        var layout = ThemeService.LoadThemeLayout(name);
        if (layout != null)
        {
            var currentTheme = AppSettings.ExtractTheme();
            currentTheme.BorderRadius = layout.BorderRadius;
            currentTheme.UsePillButtons = layout.UsePillButtons;
            currentTheme.EnableAmbientBackground = layout.EnableAmbientBackground;
            currentTheme.EnableCustomScrollbars = layout.EnableCustomScrollbars;
            currentTheme.PlayerBarPosition = layout.PlayerBarPosition;
            currentTheme.SidebarPosition = layout.SidebarPosition;
            currentTheme.PlayerBarHeight = layout.PlayerBarHeight;
            currentTheme.PlayerBarLayout = layout.PlayerBarLayout;
            currentTheme.SidebarOpacity = layout.SidebarOpacity;
            currentTheme.MainContentOpacity = layout.MainContentOpacity;
            currentTheme.PlayerBarOpacity = layout.PlayerBarOpacity;
            currentTheme.HighlightOpacity = layout.HighlightOpacity;
            currentTheme.BackgroundImagePath = layout.BackgroundImagePath;
            currentTheme.BackgroundBlur = layout.BackgroundBlur;
            currentTheme.BackgroundBrightness = layout.BackgroundBrightness;
            currentTheme.ContentGradientHeight = layout.ContentGradientHeight;
            currentTheme.CustomFontFamily = layout.CustomFontFamily;
            currentTheme.BaseFontSize = layout.BaseFontSize;
            currentTheme.CustomCss = layout.CustomCss;
            currentTheme.RowVerticalPadding = layout.RowVerticalPadding;
            currentTheme.SidebarItemPadding = layout.SidebarItemPadding;
            currentTheme.EnableZebraStriping = layout.EnableZebraStriping;
            currentTheme.ShowGridLines = layout.ShowGridLines;
            AppSettings.ApplyTheme(currentTheme);
            Save();
            StateHasChanged();
        }
    }

    private void ApplyLoadedTheme(SonorizeTheme theme)
    {
        AppSettings.ApplyTheme(theme);
        Save();
        StateHasChanged();
        RefreshThemeLists();
        _selectedColorTheme = ThemeService.GetAvailableColorThemes().FirstOrDefault() ?? "";
        _selectedLayoutTheme = ThemeService.GetAvailableLayoutThemes().FirstOrDefault() ?? "";
        StateHasChanged();
    }

    private void OpenSaveModal() => _showSaveModal = true;
    private void CloseSaveModal() => _showSaveModal = false;
    private void OpenSaveColorModal() => _showSaveColorModal = true;
    private void CloseSaveColorModal() => _showSaveColorModal = false;
    private void OpenSaveLayoutModal() => _showSaveLayoutModal = true;
    private void CloseSaveLayoutModal() => _showSaveLayoutModal = false;

    private void ConfirmSaveTheme(string name)
    {
        ThemeService.SaveTheme(name, AppSettings.ExtractTheme());
        _themeList?.RefreshThemes();
        RefreshThemeLists();
        CloseSaveModal();
    }

    private void ConfirmSaveColorScheme(string name)
    {
        var colors = new ThemeColors
        {
            AccentColor = AppSettings.Theme.AccentColor,
            BgPrimary = AppSettings.Theme.BgPrimary,
            BgSecondary = AppSettings.Theme.BgSecondary,
            BgTertiary = AppSettings.Theme.BgTertiary,
            PlayerBarBg = AppSettings.Theme.PlayerBarBg,
            BorderColor = AppSettings.Theme.BorderColor,
            TextPrimary = AppSettings.Theme.TextPrimary,
            TextSecondary = AppSettings.Theme.TextSecondary
        };
        ThemeService.SaveThemeColors(name, colors);
        RefreshThemeLists();
        CloseSaveColorModal();
    }

    private void ConfirmSaveLayoutPreset(string name)
    {
        var layout = new ThemeLayout
        {
            BorderRadius = AppSettings.Theme.BorderRadius,
            UsePillButtons = AppSettings.Theme.UsePillButtons,
            EnableAmbientBackground = AppSettings.Theme.EnableAmbientBackground,
            EnableCustomScrollbars = AppSettings.Theme.EnableCustomScrollbars,
            PlayerBarPosition = AppSettings.Theme.PlayerBarPosition,
            SidebarPosition = AppSettings.Theme.SidebarPosition,
            PlayerBarHeight = AppSettings.Theme.PlayerBarHeight,
            PlayerBarLayout = AppSettings.Theme.PlayerBarLayout,
            SidebarOpacity = AppSettings.Theme.SidebarOpacity,
            MainContentOpacity = AppSettings.Theme.MainContentOpacity,
            PlayerBarOpacity = AppSettings.Theme.PlayerBarOpacity,
            HighlightOpacity = AppSettings.Theme.HighlightOpacity,
            BackgroundImagePath = AppSettings.Theme.BackgroundImagePath,
            BackgroundBlur = AppSettings.Theme.BackgroundBlur,
            BackgroundBrightness = AppSettings.Theme.BackgroundBrightness,
            ContentGradientHeight = AppSettings.Theme.ContentGradientHeight,
            CustomFontFamily = AppSettings.Theme.CustomFontFamily,
            BaseFontSize = AppSettings.Theme.BaseFontSize,
            CustomCss = AppSettings.Theme.CustomCss,
            RowVerticalPadding = AppSettings.Theme.RowVerticalPadding,
            SidebarItemPadding = AppSettings.Theme.SidebarItemPadding,
            EnableZebraStriping = AppSettings.Theme.EnableZebraStriping,
            ShowGridLines = AppSettings.Theme.ShowGridLines
        };
        ThemeService.SaveThemeLayout(name, layout);
        RefreshThemeLists();
        CloseSaveLayoutModal();
    }

    private void Save()
    {
        SettingsManager.Save(AppSettings);
    }
}