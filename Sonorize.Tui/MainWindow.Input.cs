using System.Text;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Sonorize.Tui;

public sealed partial class MainWindow
{
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
    protected override bool OnKeyDown(Key key)
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
            if (!CloseDrillDown() && !string.IsNullOrEmpty(_search.Text))
            {
                _search.Text = string.Empty;
            }

            return true;
        }

        if (key == Key.Backspace)
        {
            return CloseDrillDown();
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
            _ = RunScanAsync(() => _library.RefreshLibraryAsync());
            return true;
        }

        if (key == Key.Delete)
        {
            if (_drillDown is null && _mode == BrowseMode.Queue)
            {
                RemoveSelectedFromQueue();
                return true;
            }

            return false;
        }

        if (key == Key.Q.WithCtrl)
        {
            App?.RequestStop();
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
                ToggleCover();
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
                SetMode((BrowseMode)(rune.Value - '1'));
                _table.SetFocus();
                return true;

            case '/':
                _search.SetFocus();
                return true;

            case 'a':
                AddMusicFolder();
                return true;

            case '?':
                MessageBox.Query(App!, "Keys", HelpText, "OK");
                return true;

            case 'q':
                App?.RequestStop();
                return true;

            default:
                return false;
        }
    }

    private void TogglePlayback()
    {
        if (!_player.IsAudioEngineAvailable)
        {
            ShowAudioEngineUnavailable();
            return;
        }

        // Nothing queued yet: treat Space like Enter on the selected song.
        if (_player.CurrentSong is null && _player.PlaybackQueue.Count == 0)
        {
            if (ShowsSongs)
            {
                ActivateSelectedRow();
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

    private void AddMusicFolder()
    {
        TextField pathField = new() { Width = Dim.Fill(0, 50), Height = 1 };

        string? entered = this.Prompt<TextField, string>(
            pathField,
            input: Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
            beginInitHandler: prompt => prompt.Title = "Add music folder");

        if (string.IsNullOrWhiteSpace(entered))
        {
            return;
        }

        string path = ExpandPath(entered.Trim());

        if (!Directory.Exists(path))
        {
            MessageBox.ErrorQuery(App!, "Add music folder", $"Folder not found:\n{path}", "OK");
            return;
        }

        if (_settings.Library.MusicFolderPaths.Contains(path))
        {
            MessageBox.Query(App!, "Add music folder", "That folder is already in your library.", "OK");
            return;
        }

        _settings.Library.MusicFolderPaths.Add(path);
        _settingsManager.Save(_settings);

        _ = RunScanAsync(() => _library.ScanFolder(path));
    }

    private void ShowAudioEngineUnavailable()
    {
        MessageBox.ErrorQuery(
            App!,
            "Audio engine unavailable",
            "The BASS native library could not be loaded, so playback is disabled.\n"
            + "Place the BASS binaries for your platform next to the executable\n"
            + "(see \"Setup Audio Dependencies\" in the README) and restart.",
            "OK");
    }

    private static string ExpandPath(string path)
    {
        if (path == "~" || path.StartsWith("~/", StringComparison.Ordinal) || path.StartsWith("~\\", StringComparison.Ordinal))
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            path = Path.Combine(home, path[1..].TrimStart('/', '\\'));
        }

        return Path.GetFullPath(path);
    }
}
