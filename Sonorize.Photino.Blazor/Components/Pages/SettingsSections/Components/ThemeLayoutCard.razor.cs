using Microsoft.AspNetCore.Components;
using Sonorize.Core.Settings;

namespace Sonorize.Photino.Blazor.Components.Pages.SettingsSections.Components;

public partial class ThemeLayoutCard
{
    private PlayerBarPosition PlayerBarPos { get => AppSettings.Theme.PlayerBarPosition; set { AppSettings.Theme.PlayerBarPosition = value; Save(); } }
    private SidebarPosition SidebarPos { get => AppSettings.Theme.SidebarPosition; set { AppSettings.Theme.SidebarPosition = value; Save(); } }
    private int BorderRadiusInt { get => AppSettings.Theme.BorderRadius; set { AppSettings.Theme.BorderRadius = value; Save(); } }
    private int PlayerBarHeightInt { get => AppSettings.Theme.PlayerBarHeight; set { AppSettings.Theme.PlayerBarHeight = value; Save(); } }
    private int BaseFontSizeInt { get => AppSettings.Theme.BaseFontSize; set { AppSettings.Theme.BaseFontSize = value; Save(); } }

    private void UpdateBool(Action<bool> updateAction, ChangeEventArgs e) { updateAction((bool)(e.Value ?? false)); Save(); }
    private void Save()
    {
        SettingsManager.Save(AppSettings);
    }
}