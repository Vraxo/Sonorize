using Microsoft.AspNetCore.Components;
using Sonorize.Core;
using Sonorize.Core.Models;

namespace Sonorize.Photino.Blazor.Components.Pages.SettingsSections;

public partial class GeneralTab
{
    private List<AudioDeviceInfo> _audioDevices = [];

    protected override void OnInitialized()
    {
        _audioDevices = PlayerService.GetAvailableAudioDevices();
    }

    private bool IsSelected(AudioDeviceInfo device)
    {
        var storedIndex = AppSettings.Playback.OutputDeviceName;
        return string.IsNullOrEmpty(storedIndex) ? device.IsDefault : storedIndex == device.Index.ToString();
    }

    private void OnDeviceChanged(ChangeEventArgs e)
    {
        string? val = e.Value?.ToString();
        AppSettings.Playback.OutputDeviceName = val;
        PlayerService.ChangeOutputDevice(val);
        Save();
    }

    private bool PlayOnSingleClick { get => AppSettings.Playback.PlayOnSingleClick; set { AppSettings.Playback.PlayOnSingleClick = value; Save(); } }
    private bool EnableZebra { get => AppSettings.Theme.EnableZebraStriping; set { AppSettings.Theme.EnableZebraStriping = value; Save(); } }
    private bool ShowGridLines { get => AppSettings.Theme.ShowGridLines; set { AppSettings.Theme.ShowGridLines = value; Save(); } }

    private int RowPadding { get => AppSettings.Theme.RowVerticalPadding; set { AppSettings.Theme.RowVerticalPadding = value; Save(); } }
    private int SidebarItemPadding { get => AppSettings.Theme.SidebarItemPadding; set { AppSettings.Theme.SidebarItemPadding = value; Save(); } }
    private int ListArtSize { get => AppSettings.Library.ListArtSize; set { AppSettings.Library.ListArtSize = value; Save(); } }

    private bool ShowThumbs { get => AppSettings.Window.ShowSliderThumbs; set { AppSettings.Window.ShowSliderThumbs = value; Save(); } }
    private bool EnableSidebarAnimation { get => AppSettings.Window.EnableSidebarAnimation; set { AppSettings.Window.EnableSidebarAnimation = value; Save(); } }

    private int ZoomInt
    {
        get => (int)(AppSettings.Window.ZoomLevel * 100);
        set { AppSettings.Window.ZoomLevel = value / 100.0; Save(); }
    }

    private bool ShowIndex { get => AppSettings.Library.Columns.ShowIndex; set { AppSettings.Library.Columns.ShowIndex = value; Save(); } }
    private bool ShowArt { get => AppSettings.Library.Columns.ShowArt; set { AppSettings.Library.Columns.ShowArt = value; Save(); } }
    private bool ShowArtist { get => AppSettings.Library.Columns.ShowArtist; set { AppSettings.Library.Columns.ShowArtist = value; Save(); } }
    private bool ShowAlbum { get => AppSettings.Library.Columns.ShowAlbum; set { AppSettings.Library.Columns.ShowAlbum = value; Save(); } }
    private bool ShowDuration { get => AppSettings.Library.Columns.ShowDuration; set { AppSettings.Library.Columns.ShowDuration = value; Save(); } }

    private void UpdateOverflow(TextOverflowMode mode) { AppSettings.Library.TrackListOverflow = mode; Save(); }
    private void Save()
    {
        SettingsManager.Save(AppSettings);
    }
}