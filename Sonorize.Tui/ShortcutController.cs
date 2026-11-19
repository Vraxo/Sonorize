using System.Text;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Sonorize.Tui;

internal sealed class ShortcutController
{
    private readonly Sonorize.Core.Playback.IPlayerService _player;
    private readonly Sonorize.Core.Library.LibraryService _library;
    private readonly LibraryScanRunner _scans;
    private readonly BrowseController _browse;
    private readonly CoverArtController _cover;
    private readonly MusicFolderDialog _folderDialog;
    private readonly AudioEngineNotifier _audioNotifier;
    private readonly Func<Terminal.Gui.App.IApplication?> _appProvider;
    private readonly TextField _search;
    private readonly TableView _table;

    public ShortcutController(Sonorize.Core.Playback.IPlayerService player,
        Sonorize.Core.Library.LibraryService library, LibraryScanRunner scans,
        BrowseController browse, CoverArtController cover, MusicFolderDialog folderDialog,
        AudioEngineNotifier audioNotifier, MainWindowLayout layout,
        Func<Terminal.Gui.App.IApplication?> appProvider)
    {
        _player = player;
        _library = library;
        _scans = scans;
        _browse = browse;
        _cover = cover;
        _folderDialog = folderDialog;
        _audioNotifier = audioNotifier;
        _appProvider = appProvider;
        _search = layout.Search;
        _table = layout.Table;
        _search.KeyDown += OnSearchKeyDown;
        layout.Sidebar.KeyDown += OnBrowseKeyDown;
        _table.KeyDown += OnBrowseKeyDown;
    }

    private const double SeekStepSeconds = 5;
    private const float VolumeStep = 0.05f;

    private const string HelpText =
        """
        Enter        Play song / open artist or album
        Space        Play / pause
        n  p         Next / previous track
        ←  →         Seek 5 seconds
        +  -         Volume
        s            Toggle shuffle
        r            Cycle repeat (off, all, one)
        c            Show / hide cover art

        1 2 3 4      Songs / Artists / Albums / Queue
        Tab          Move between search, sidebar and list
        /            Search
        Esc          Back / clear search
        Delete       Remove from queue (Queue view)

        a            Add a music folder
        F5           Rescan library
        q            Quit
        """;

    /// <summary>
    /// The focused view sees a key before its SuperView does, and both lists would
    /// otherwise swallow Space and the arrow keys, so the shortcuts hook KeyDown
    /// on the sidebar and the table directly.
    /// </summary>
    private void OnBrowseKeyDown(object? sender, Key key)
    {
        if (HandleShortcut(key))
        {
            key.Handled = true;
        }
    }

    /// <summary>Fallback for when neither list has focus.</summary>
    public bool OnKeyDown(Key key)
    {
        // While typing in the search box every printable key is text.
        return !_search.HasFocus && HandleShortcut(key);
    }

    private void OnSearchKeyDown(object? sender, Key key)
    {
        if (key == Key.Esc)
        {
            if (string.IsNullOrEmpty(_search.Text))
            {
                _table.SetFocus();
            }
            else
            {
                _search.Text = string.Empty;
            }

            key.Handled = true;
        }
        else if (key == Key.Enter || key == Key.CursorDown)
        {
            _table.SetFocus();
            key.Handled = true;
        }
    }

    private bool HandleShortcut(Key key)
    {
        if (key == Key.Esc)
        {
            // Always consumed: Esc is Terminal.Gui's default quit key, and losing
            // the player to a stray Esc is unfriendly. Quit is q / Ctrl+Q.
            if (!_browse.CloseDrillDown() && !string.IsNullOrEmpty(_search.Text))
            {
                _search.Text = string.Empty;
            }

            return true;
        }

        if (key == Key.Backspace)
        {
            return _browse.CloseDrillDown();
        }

        if (key == Key.CursorLeft)
        {
            SeekBy(-SeekStepSeconds);
            return true;
        }

        if (key == Key.CursorRight)
        {
            SeekBy(SeekStepSeconds);
            return true;
        }

        if (key == Key.F5)
        {
            _ = _scans.RunScanAsync(() => _library.RefreshLibraryAsync());
            return true;
        }

        if (key == Key.Delete)
        {
            if (_browse.IsQueueView)
            {
                _browse.RemoveSelectedFromQueue();
                return true;
            }

            return false;
        }

        if (key == Key.Q.WithCtrl)
        {
            _appProvider()?.RequestStop();
            return true;
        }

        if (key.IsCtrl || key.IsAlt)
        {
            return false;
        }

        Rune rune = key.AsRune;

        if (!rune.IsBmp)
        {
            return false;
        }

        switch (char.ToLowerInvariant((char)rune.Value))
        {
            case ' ':
                TogglePlayback();
                return true;

            case 'n':
                _player.PlayNext();
                return true;

            case 'p':
                _player.PlayPrevious();
                return true;

            case 's':
                _player.ToggleShuffle();
                return true;

            case 'r':
                _player.ToggleRepeat();
                return true;

            case 'c':
                _cover.ToggleCover();
                return true;

            case '+':
            case '=':
                _player.SetVolume(_player.Volume + VolumeStep);
                return true;

            case '-':
            case '_':
                _player.SetVolume(_player.Volume - VolumeStep);
                return true;

            case '1':
            case '2':
            case '3':
            case '4':
                _browse.SetMode((BrowseMode)(rune.Value - '1'));
                _table.SetFocus();
                return true;

            case '/':
                _search.SetFocus();
                return true;

            case 'a':
                _folderDialog.Show();
                return true;

            case '?':
                MessageBox.Query(_appProvider()!, "Keys", HelpText, "OK");
                return true;

            case 'q':
                _appProvider()?.RequestStop();
                return true;

            default:
                return false;
        }
    }

    private void TogglePlayback()
    {
        if (!_player.IsAudioEngineAvailable)
        {
            _audioNotifier.ShowUnavailable();
            return;
        }

        // Nothing queued yet: treat Space like Enter on the selected song.
        if (_player.CurrentSong is null && _player.PlaybackQueue.Count == 0)
        {
            if (_browse.ShowsSongs)
            {
                _browse.ActivateSelectedRow();
            }

            return;
        }

        _ = _player.TogglePlayback();
    }

    private void SeekBy(double seconds)
    {
        TimeSpan total = _player.TotalTime;

        if (total <= TimeSpan.Zero)
        {
            return;
        }

        double target = _player.CurrentTime.TotalSeconds + seconds;

        // IPlayerService.Seek takes a 0..1 fraction of the track.
        _player.Seek(Math.Clamp(target / total.TotalSeconds, 0d, 1d));
    }

}
