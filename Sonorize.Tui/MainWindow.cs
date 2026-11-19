using Sonorize.Core.Core;
using Sonorize.Core.Library;
using Sonorize.Core.Playback;
using Sonorize.Core.Settings;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Sonorize.Tui;

/// <summary>Composes the terminal frontend's views and controllers.</summary>
public sealed class MainWindow : Runnable
{
    private readonly MainWindowLayout _layout;
    private readonly BrowseController _browse;
    private readonly NowPlayingController _nowPlaying;
    private readonly CoverArtController _cover;
    private readonly ShortcutController _shortcuts;
    private readonly LibraryScanRunner _scans;
    private readonly UiUpdateCoordinator _updates;

    public MainWindow(LibraryService library, IPlayerService player, SonorizeSettings settings,
        ISettingsManager<SonorizeSettings> settingsManager, LogService logger)
    {
        Title = "Sonorize";
        _layout = new MainWindowLayout();
        Add(_layout.Views.ToArray());

        _updates = new UiUpdateCoordinator(library, player);
        _scans = new LibraryScanRunner(library, settings, logger, _updates.MarkLibraryChanged);
        AudioEngineNotifier notifier = new(() => App);
        _browse = new BrowseController(library, player, _layout, notifier);
        _cover = new CoverArtController(player, _layout);
        _nowPlaying = new NowPlayingController(player, _scans, _layout);
        MusicFolderService folders = new(settings, settingsManager,
            path => _ = _scans.RunScanAsync(() => library.ScanFolder(path)));
        MusicFolderDialog folderDialog = new(this, folders);
        _shortcuts = new ShortcutController(player, library, _scans, _browse, _cover, folderDialog, notifier, _layout, () => App);
        _nowPlaying.UpdateNowPlaying();
    }

    protected override bool OnKeyDown(Key key) => _shortcuts.OnKeyDown(key);

    protected override void OnIsRunningChanged(bool newIsRunning)
    {
        base.OnIsRunningChanged(newIsRunning);
        if (newIsRunning)
            _updates.Start(App!, _layout, _browse, _nowPlaying, _cover, _scans);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _updates.Dispose();
        base.Dispose(disposing);
    }
}
