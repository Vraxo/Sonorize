using Sonorize.Core.Library;
using Sonorize.Core.Playback;
using Terminal.Gui.App;

namespace Sonorize.Tui;

/// <summary>Marshals background change notifications onto the UI timer and owns its lifetime.</summary>
internal sealed class UiUpdateCoordinator : IDisposable
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(200);
    private readonly LibraryService _library;
    private readonly IPlayerService _player;
    private int _libraryDirty;
    private int _queueDirty;
    private IApplication? _app;
    private object? _tickToken;
    private bool _closing;

    public UiUpdateCoordinator(LibraryService library, IPlayerService player)
    {
        _library = library;
        _player = player;
        _library.LibraryChanged += MarkLibraryChanged;
        _player.QueueChanged += OnQueueChanged;
    }

    public void MarkLibraryChanged() => Interlocked.Exchange(ref _libraryDirty, 1);
    private void OnQueueChanged() => Interlocked.Exchange(ref _queueDirty, 1);

    public void Start(IApplication app, MainWindowLayout layout, BrowseController browse,
        NowPlayingController nowPlaying, CoverArtController cover, LibraryScanRunner scans)
    {
        if (_closing || _tickToken is not null)
            return;

        layout.Table.SetFocus();
        _app = app;
        _tickToken = app.AddTimeout(TickInterval, () =>
        {
            if (_closing)
                return false;

            bool libraryChanged = Interlocked.Exchange(ref _libraryDirty, 0) == 1;
            bool queueChanged = Interlocked.Exchange(ref _queueDirty, 0) == 1;
            if (libraryChanged || (queueChanged && browse.IsQueueView))
                browse.RefreshRows(keepSelection: true);

            nowPlaying.UpdateNowPlaying();
            layout.UpdateFocusBorders();
            cover.UpdateCover();
            return true;
        });
        _ = scans.LoadLibraryAsync();
    }

    public void Dispose()
    {
        if (_closing)
            return;

        _closing = true;
        _library.LibraryChanged -= MarkLibraryChanged;
        _player.QueueChanged -= OnQueueChanged;
        if (_tickToken is not null)
        {
            _app?.RemoveTimeout(_tickToken);
            _tickToken = null;
        }
    }
}
