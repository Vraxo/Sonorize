using NativeFileDialogs.Net;
using Sonorize.Core.Services.Library;

namespace Sonorize.Photino.Blazor.Components.Pages.SettingsSections;

public partial class LibraryTab
{
    private bool ScanOnStartup
    {
        get => AppSettings.Library.ScanOnStartup;

        set
        {
            AppSettings.Library.ScanOnStartup = value;
            Save();
        }
    }

    private int GridWidth
    {
        get => AppSettings.Library.GridItemWidth;

        set
        {
            AppSettings.Library.GridItemWidth = value;
            Save();
        }
    }

    private int GridGap
    {
        get => AppSettings.Library.GridGap;

        set
        {
            AppSettings.Library.GridGap = value;
            Save();
        }
    }

    private int GridPadding
    {
        get => AppSettings.Library.GridItemPadding;
        set
        {
            AppSettings.Library.GridItemPadding = value;
            Save();
        }
    }

    private void AddFolder()
    {
        NfdStatus result = Nfd.PickFolder(out string? path, null);

        if (result != NfdStatus.Ok || string.IsNullOrEmpty(path) || AppSettings.Library.MusicFolderPaths.Contains(path))
        {
            return;
        }

        AppSettings.Library.MusicFolderPaths.Add(path);
        Save();
        // Just scan this new folder
        _ = LibraryService.ScanFolder(path);
    }

    private void RemoveFolder(string path)
    {
        _ = AppSettings.Library.MusicFolderPaths.Remove(path);
        Save();
        // Trigger full reconciliation to remove zombie tracks
        _ = LibraryService.RefreshLibraryAsync();
    }

    private void Save()
    {
        SettingsManager.Save(AppSettings);
    }
}