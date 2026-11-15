using System.Collections.ObjectModel;
using Sonorize.Core.Core;
using Sonorize.Core.Library;
using Sonorize.Core.Playback;
using Sonorize.Core.Settings;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Sonorize.Tui;

/// <summary>
/// The single full-screen view of the terminal frontend: search box, sidebar,
/// cover art, track table and the now-playing bar.
/// </summary>
/// <remarks>
/// Split across partial files: this one owns layout and lifetime,
/// <c>MainWindow.Browse.cs</c> fills the table, <c>MainWindow.Input.cs</c> handles
/// keys, <c>MainWindow.NowPlaying.cs</c> the playback bar and
/// <c>MainWindow.Cover.cs</c> the album art.
/// </remarks>
public sealed partial class MainWindow : Runnable
{
    // Wide enough for a roughly square cover: terminal cells are about twice as tall as wide.
    private const int SidebarWidth = 22;

    // Four entries plus the border.
    private const int SidebarListHeight = 6;
    private const int FooterHeight = 4;

    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(200);

    private readonly LibraryService _library;
    private readonly IPlayerService _player;
    private readonly SonorizeSettings _settings;
    private readonly ISettingsManager<SonorizeSettings> _settingsManager;
    private readonly LogService _logger;

    private readonly TextField _search;
    private readonly ListView _sidebar;
    private readonly TableView _table;
    private readonly ImageView _cover;
    private readonly Label _emptyHint;
    private readonly Label _nowPlaying;
    private readonly ProgressBar _progress;
    private readonly Label _time;
    private readonly Label _status;
    private readonly Label _shuffle;
    private readonly Label _repeat;

    // Core raises its events on timer and thread-pool threads. The handlers only
    // set these flags; the UI timer (OnTick) reads them on the UI thread.
    private int _libraryDirty;
    private int _queueDirty;
    private int _activeScans;

    private object? _tickToken;
    private bool _closing;
    private bool _sidebarBorderFocused;
    private bool _tableBorderFocused;

    public MainWindow(
        LibraryService library,
        IPlayerService player,
        SonorizeSettings settings,
        ISettingsManager<SonorizeSettings> settingsManager,
        LogService logger)
    {
        _library = library;
        _player = player;
        _settings = settings;
        _settingsManager = settingsManager;
        _logger = logger;

        Title = "Sonorize";

        Label searchLabel = new() { Text = " Search: ", X = 0, Y = 0 };

        _search = new TextField
        {
            X = Pos.Right(searchLabel),
            Y = 0,
            Width = Dim.Fill(1)
        };

        _sidebar = new ListView
        {
            Title = "Library",
            BorderStyle = LineStyle.Rounded,
            X = 0,
            Y = 1,
            Width = SidebarWidth,
            Height = SidebarListHeight,

            // Letters are playback shortcuts here, not type-to-search.
            KeystrokeNavigator = null
        };
        _sidebar.SetSource(new ObservableCollection<string>(Enum.GetNames<BrowseMode>()));

        // Draws real pixels on terminals with Kitty graphics or Sixel, and
        // coloured cells everywhere else.
        _cover = new ImageView
        {
            Title = "Cover",
            BorderStyle = LineStyle.Rounded,
            X = 0,
            Y = Pos.Bottom(_sidebar),
            Width = SidebarWidth,
            Height = Dim.Fill(FooterHeight),
            CanFocus = false
        };

        _table = new TableView
        {
            Title = "Songs",
            BorderStyle = LineStyle.Rounded,
            X = Pos.Right(_sidebar),
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill(FooterHeight),
            FullRowSelect = true,
            MultiSelect = false,
            CollectionNavigator = null
        };
        _table.Style.ShowHorizontalHeaderOverline = false;
        _table.Style.ShowVerticalCellLines = false;
        _table.Style.ShowVerticalHeaderLines = false;
        _table.Style.AlwaysShowHeaders = true;
        _table.Style.HeaderScheme = TuiColors.Accent;
        _table.Style.RowColorGetter = args => IsPlayingRow(args.RowIndex) ? TuiColors.Playing : null;

        _emptyHint = new Label
        {
            X = Pos.Center(),
            Y = Pos.Center(),
            Visible = false
        };

        _nowPlaying = new Label
        {
            X = 1,
            Y = Pos.AnchorEnd(FooterHeight),
            Width = Dim.Fill(1),
            Height = 1
        };

        _time = new Label
        {
            X = Pos.AnchorEnd(TimeLabelWidth + 1),
            Y = Pos.AnchorEnd(FooterHeight - 1),
            Width = TimeLabelWidth,
            Height = 1
        };

        _progress = new ProgressBar
        {
            X = 1,
            Y = Pos.AnchorEnd(FooterHeight - 1),
            Width = Dim.Fill(TimeLabelWidth + 3),
            Height = 1
        };

        _shuffle = new Label
        {
            X = 1,
            Y = Pos.AnchorEnd(FooterHeight - 2),
            Width = ShuffleLabelWidth,
            Height = 1
        };

        _repeat = new Label
        {
            X = Pos.Right(_shuffle) + StatusGap,
            Y = Pos.AnchorEnd(FooterHeight - 2),
            Width = RepeatLabelWidth,
            Height = 1
        };

        _status = new Label
        {
            X = Pos.Right(_repeat) + StatusGap,
            Y = Pos.AnchorEnd(FooterHeight - 2),
            Width = Dim.Fill(1),
            Height = 1
        };

        Label hints = new()
        {
            Text = "Enter Play  Space Pause  n/p Next/Prev  ←/→ Seek  +/- Volume  / Search  a Add folder  ? Help  q Quit",
            X = 1,
            Y = Pos.AnchorEnd(FooterHeight - 3),
            Width = Dim.Fill(1),
            Height = 1
        };

        searchLabel.SetScheme(TuiColors.Accent);
        _emptyHint.SetScheme(TuiColors.Muted);
        _nowPlaying.SetScheme(TuiColors.Playing);
        _progress.SetScheme(TuiColors.Accent);
        _time.SetScheme(TuiColors.Muted);
        _status.SetScheme(TuiColors.Muted);
        hints.SetScheme(TuiColors.Muted);

        Add(searchLabel, _search, _sidebar, _cover, _table, _emptyHint, _nowPlaying, _progress, _time, _shuffle, _repeat, _status, hints);

        _search.TextChanged += OnSearchTextChanged;
        _search.KeyDown += OnSearchKeyDown;

        _sidebar.ValueChanged += OnSidebarValueChanged;
        _sidebar.Accepting += OnSidebarAccepting;
        _sidebar.KeyDown += OnBrowseKeyDown;

        _table.Accepting += OnTableAccepting;
        _table.KeyDown += OnBrowseKeyDown;
        _table.ViewportChanged += OnTableViewportChanged;

        _library.LibraryChanged += OnLibraryChanged;
        _player.QueueChanged += OnQueueChanged;

        _sidebar.Value = (int)BrowseMode.Songs;
        RefreshRows(keepSelection: false);
        UpdateNowPlaying();
    }

    protected override void OnIsRunningChanged(bool newIsRunning)
    {
        base.OnIsRunningChanged(newIsRunning);

        // Guard against a second start: one timer and one initial load only.
        if (!newIsRunning || _tickToken is not null)
        {
            return;
        }

        _table.SetFocus();
        _tickToken = App!.AddTimeout(TickInterval, OnTick);
        _ = LoadLibraryAsync();
    }

    /// <summary>Runs on the UI thread; applies whatever the background events flagged.</summary>
    private bool OnTick()
    {
        if (_closing)
        {
            return false;
        }

        bool libraryChanged = Interlocked.Exchange(ref _libraryDirty, 0) == 1;
        bool queueChanged = Interlocked.Exchange(ref _queueDirty, 0) == 1;

        if (libraryChanged || (queueChanged && _mode == BrowseMode.Queue && _drillDown is null))
        {
            RefreshRows(keepSelection: true);
        }

        UpdateNowPlaying();
        UpdateFocusBorders();
        UpdateCover();

        return true;
    }

    /// <summary>Colours the border of whichever pane has keyboard focus.</summary>
    private void UpdateFocusBorders()
    {
        bool sidebarFocused = _sidebar.HasFocus;
        bool tableFocused = _table.HasFocus;

        if (sidebarFocused != _sidebarBorderFocused)
        {
            _sidebarBorderFocused = sidebarFocused;
            _sidebar.Border.GetOrCreateView().SetScheme(sidebarFocused ? TuiColors.FocusedBorder : null);
        }

        if (tableFocused != _tableBorderFocused)
        {
            _tableBorderFocused = tableFocused;
            _table.Border.GetOrCreateView().SetScheme(tableFocused ? TuiColors.FocusedBorder : null);
        }
    }

    private void OnLibraryChanged()
    {
        Interlocked.Exchange(ref _libraryDirty, 1);
    }

    private void OnQueueChanged()
    {
        Interlocked.Exchange(ref _queueDirty, 1);
    }

    private async Task LoadLibraryAsync()
    {
        await RunScanAsync(async () =>
        {
            await _library.InitializeAsync().ConfigureAwait(false);

            if (_settings.Library.ScanOnStartup && _settings.Library.MusicFolderPaths.Count > 0)
            {
                await _library.RefreshLibraryAsync().ConfigureAwait(false);
            }
        }).ConfigureAwait(false);
    }

    /// <summary>Runs a library operation off the UI thread while the status line shows "Scanning".</summary>
    private async Task RunScanAsync(Func<Task> scan)
    {
        Interlocked.Increment(ref _activeScans);

        try
        {
            await Task.Run(scan).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.Error("[TUI] Library scan failed", ex);
        }
        finally
        {
            Interlocked.Decrement(ref _activeScans);
            Interlocked.Exchange(ref _libraryDirty, 1);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _closing = true;
            _library.LibraryChanged -= OnLibraryChanged;
            _player.QueueChanged -= OnQueueChanged;

            if (_tickToken is not null)
            {
                App?.RemoveTimeout(_tickToken);
                _tickToken = null;
            }
        }

        base.Dispose(disposing);
    }
}
