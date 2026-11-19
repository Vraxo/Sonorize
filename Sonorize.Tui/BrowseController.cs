using Sonorize.Core.Core;
using Sonorize.Core.Library;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Sonorize.Tui;

internal sealed class BrowseController
{
    private readonly LibraryService _library;
    private readonly Sonorize.Core.Playback.IPlayerService _player;
    private readonly TextField _search;
    private readonly ListView _sidebar;
    private readonly TableView _table;
    private readonly Label _emptyHint;
    private readonly AudioEngineNotifier _audioNotifier;

    public BrowseController(LibraryService library, Sonorize.Core.Playback.IPlayerService player,
        MainWindowLayout layout, AudioEngineNotifier audioNotifier)
    {
        _library = library;
        _player = player;
        _search = layout.Search;
        _sidebar = layout.Sidebar;
        _table = layout.Table;
        _emptyHint = layout.EmptyHint;
        _audioNotifier = audioNotifier;

        _table.Style.RowColorGetter = args => IsPlayingRow(args.RowIndex) ? TuiColors.Playing : null;
        _search.TextChanged += OnSearchTextChanged;
        _sidebar.ValueChanged += OnSidebarValueChanged;
        _sidebar.Accepting += OnSidebarAccepting;
        _table.Accepting += OnTableAccepting;
        _table.ViewportChanged += OnTableViewportChanged;
        _sidebar.Value = (int)BrowseMode.Songs;
        RefreshRows(keepSelection: false);
    }

    public bool IsQueueView => _drillDown is null && _mode == BrowseMode.Queue;

    /// <summary>The songs of one artist or album, opened from the Artists/Albums lists.</summary>
    private sealed record DrillDown(string Title, Func<Song, bool> Filter);

    private BrowseMode _mode = BrowseMode.Songs;
    private DrillDown? _drillDown;

    // Exactly one of these backs the table at a time; the others are empty.
    private List<Song> _songRows = [];
    private List<ArtistGroup> _artistRows = [];
    private List<AlbumGroup> _albumRows = [];

    private bool _suppressSearchRefresh;
    private int _lastTableWidth = -1;

    public bool ShowsSongs => _drillDown is not null || _mode is BrowseMode.Songs or BrowseMode.Queue;

    private bool IsPlayingRow(int row)
    {
        return ShowsSongs
            && row >= 0
            && row < _songRows.Count
            && _songRows[row].FilePath == _player.CurrentSong?.FilePath;
    }

    private int SelectedRow => _table.Value?.SelectedCell.Y ?? -1;

    public void SetMode(BrowseMode mode)
    {
        if (_mode == mode && _drillDown is null)
        {
            return;
        }

        _mode = mode;
        _drillDown = null;

        if (_sidebar.Value != (int)mode)
        {
            _sidebar.Value = (int)mode;
        }

        ClearSearchAndRefresh();
    }

    private void OpenDrillDown(DrillDown drillDown)
    {
        _drillDown = drillDown;
        ClearSearchAndRefresh();
    }

    public bool CloseDrillDown()
    {
        if (_drillDown is null)
        {
            return false;
        }

        _drillDown = null;
        ClearSearchAndRefresh();
        return true;
    }

    private void ClearSearchAndRefresh()
    {
        _suppressSearchRefresh = true;
        _search.Text = string.Empty;
        _suppressSearchRefresh = false;

        RefreshRows(keepSelection: false);
    }

    public void RefreshRows(bool keepSelection)
    {
        int previousRow = SelectedRow;
        int previousOffset = _table.RowOffset;
        string query = (_search.Text ?? string.Empty).Trim();

        _songRows = [];
        _artistRows = [];
        _albumRows = [];

        string title;
        ITableSource source;

        if (_drillDown is { } drillDown)
        {
            IEnumerable<Song> songs = _library.AllSongs
                .Where(drillDown.Filter)
                .OrderBy(s => s.Album, StringComparer.OrdinalIgnoreCase)
                .ThenBy(s => s.Title, StringComparer.OrdinalIgnoreCase);

            _songRows = [.. SearchService.Search(songs, query)];
            title = drillDown.Title;
            source = BuildSongSource();
        }
        else
        {
            switch (_mode)
            {
                case BrowseMode.Artists:
                    _artistRows = [.. _library.AllArtists.Where(a => Matches(query, a.Name))];
                    title = "Artists";
                    source = BuildArtistSource();
                    break;

                case BrowseMode.Albums:
                    _albumRows = [.. _library.AllAlbums.Where(a => Matches(query, a.Title, a.Artist))];
                    title = "Albums";
                    source = BuildAlbumSource();
                    break;

                case BrowseMode.Queue:
                    _songRows = [.. SearchService.Search(_player.PlaybackQueue.ToArray(), query)];
                    title = "Queue";
                    source = BuildSongSource();
                    break;

                default:
                    _songRows = [.. SearchService.Search(_library.AllSongs, query)];
                    title = "Songs";
                    source = BuildSongSource();
                    break;
            }
        }

        ApplyColumnWidths();
        _table.Table = source;
        _table.Title = $"{title} ({source.Rows})";

        if (keepSelection && source.Rows > 0 && previousRow >= 0)
        {
            _table.RowOffset = Math.Min(previousOffset, source.Rows - 1);
            _table.SetSelection(0, Math.Min(previousRow, source.Rows - 1), false);
            _table.Update();
        }

        UpdateEmptyHint(source.Rows, query);
    }

    private EnumerableTableSource<Song> BuildSongSource()
    {
        return new EnumerableTableSource<Song>(
            _songRows,
            new Dictionary<string, Func<Song, object>>
            {
                // Evaluated on every draw, so the marker follows playback without a rebuild.
                { " ", s => s.FilePath == _player.CurrentSong?.FilePath ? "▶" : " " },
                { "Title", s => Cell(s.Title) },
                { "Artist", s => Cell(s.Artist) },
                { "Album", s => Cell(s.Album) },
                { "Time", s => s.DurationString }
            });
    }

    private EnumerableTableSource<ArtistGroup> BuildArtistSource()
    {
        return new EnumerableTableSource<ArtistGroup>(
            _artistRows,
            new Dictionary<string, Func<ArtistGroup, object>>
            {
                { "Artist", a => Cell(a.Name) },
                { "Albums", a => a.AlbumCount },
                { "Songs", a => a.SongCount }
            });
    }

    private EnumerableTableSource<AlbumGroup> BuildAlbumSource()
    {
        return new EnumerableTableSource<AlbumGroup>(
            _albumRows,
            new Dictionary<string, Func<AlbumGroup, object>>
            {
                { "Album", a => Cell(a.Title) },
                { "Artist", a => Cell(a.Artist) },
                { "Songs", a => a.SongCount }
            });
    }

    /// <summary>
    /// TableView sizes columns to their content. Pin every column but the last
    /// (which expands) so the layout stays put while scrolling.
    /// </summary>
    private void ApplyColumnWidths()
    {
        int width = _table.Viewport.Width;
        _lastTableWidth = width;
        _table.Style.ColumnStyles.Clear();

        if (ShowsSongs)
        {
            // marker (1) + time (6) + one separator per column (5) + slack (2)
            int flexible = width - 14;

            if (flexible < 15)
            {
                return;
            }

            int titleWidth = flexible * 40 / 100;
            int artistWidth = flexible * 30 / 100;

            PinColumn(0, 1);
            PinColumn(1, titleWidth);
            PinColumn(2, artistWidth).ColorGetter = ArtistCellScheme;
            PinColumn(3, flexible - titleWidth - artistWidth);
        }
        else if (_mode == BrowseMode.Artists)
        {
            // albums (6) + songs (6) + separators (3) + slack (2)
            int flexible = width - 17;

            if (flexible < 10)
            {
                return;
            }

            PinColumn(0, flexible);
            PinColumn(1, 6);
        }
        else
        {
            // songs (6) + separators (3) + slack (2)
            int flexible = width - 11;

            if (flexible < 15)
            {
                return;
            }

            int albumWidth = flexible * 55 / 100;

            PinColumn(0, albumWidth);
            PinColumn(1, flexible - albumWidth).ColorGetter = ArtistCellScheme;
        }
    }

    private ColumnStyle PinColumn(int column, int width)
    {
        ColumnStyle style = _table.Style.GetOrCreateColumnStyle(column);
        style.MinWidth = width;
        style.MaxWidth = width;

        return style;
    }

    private Scheme? _artistCellRow;
    private Scheme? _artistCell;

    /// <summary>Artist names in purple, except on the playing row, which stays one colour.</summary>
    private Scheme? ArtistCellScheme(CellColorGetterArgs args)
    {
        if (args.RowScheme == TuiColors.Playing)
        {
            return null;
        }

        // Called for every visible row on every draw: rebuild only when the table's scheme changes.
        if (_artistCell is null || args.RowScheme != _artistCellRow)
        {
            _artistCellRow = args.RowScheme;
            _artistCell = TuiColors.ArtistCell(args.RowScheme);
        }

        return _artistCell;
    }

    private void UpdateEmptyHint(int rows, string query)
    {
        if (rows > 0)
        {
            _emptyHint.Visible = false;
            return;
        }

        if (query.Length > 0)
        {
            _emptyHint.Text = "No matches.";
        }
        else if (_drillDown is null && _mode == BrowseMode.Queue)
        {
            _emptyHint.Text = "The queue is empty. Press Enter on a song to start playing.";
        }
        else if (_library.AllSongs.Count == 0)
        {
            _emptyHint.Text = "Your library is empty. Press a to add a music folder.";
        }
        else
        {
            _emptyHint.Text = "Nothing here.";
        }

        _emptyHint.Visible = true;
    }

    /// <summary>Plays the selected song, or opens the selected artist/album.</summary>
    public void ActivateSelectedRow()
    {
        int row = SelectedRow;

        if (row < 0)
        {
            return;
        }

        if (ShowsSongs)
        {
            if (row < _songRows.Count)
            {
                PlayFromRows(_songRows[row]);
            }

            return;
        }

        if (_mode == BrowseMode.Artists && row < _artistRows.Count)
        {
            string artist = _artistRows[row].Name;
            OpenDrillDown(new DrillDown(Cell(artist), s => s.Artist == artist));
        }
        else if (_mode == BrowseMode.Albums && row < _albumRows.Count)
        {
            // Albums are grouped by title + artist, so match on both.
            AlbumGroup album = _albumRows[row];
            OpenDrillDown(new DrillDown(
                $"{Cell(album.Title)} — {Cell(album.Artist)}",
                s => s.Album == album.Title && s.Artist == album.Artist));
        }
    }

    private void PlayFromRows(Song song)
    {
        if (!_player.IsAudioEngineAvailable)
        {
            _audioNotifier.ShowUnavailable();
            return;
        }

        bool inQueueView = _drillDown is null && _mode == BrowseMode.Queue;

        // In the queue view, jump within the existing queue. Everywhere else the
        // visible list becomes the new queue (a copy: the player keeps the list).
        List<Song> context = inQueueView ? _player.PlaybackQueue : [.. _songRows];

        _ = _player.PlaySong(song, context);
    }

    public void RemoveSelectedFromQueue()
    {
        int row = SelectedRow;

        if (row < 0 || row >= _songRows.Count)
        {
            return;
        }

        // Rows may be filtered by the search box, so resolve the real queue index.
        int queueIndex = _player.PlaybackQueue.IndexOf(_songRows[row]);

        if (queueIndex >= 0)
        {
            _player.RemoveFromQueue(queueIndex);
            RefreshRows(keepSelection: true);
        }
    }

    private void OnSearchTextChanged(object? sender, EventArgs e)
    {
        if (!_suppressSearchRefresh)
        {
            RefreshRows(keepSelection: false);
        }
    }

    private void OnSidebarValueChanged(object? sender, ValueChangedEventArgs<int?> e)
    {
        if (e.NewValue is int index && Enum.IsDefined((BrowseMode)index))
        {
            SetMode((BrowseMode)index);
        }
    }

    private void OnSidebarAccepting(object? sender, CommandEventArgs e)
    {
        _table.SetFocus();
        e.Handled = true;
    }

    private void OnTableAccepting(object? sender, CommandEventArgs e)
    {
        ActivateSelectedRow();
        e.Handled = true;
    }

    private void OnTableViewportChanged(object? sender, DrawEventArgs e)
    {
        if (_table.Viewport.Width == _lastTableWidth)
        {
            return;
        }

        ApplyColumnWidths();
        _table.Update();
    }

    private static bool Matches(string query, params string?[] fields)
    {
        if (query.Length == 0)
        {
            return true;
        }

        string[] tokens = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return tokens.All(token => fields.Any(f => f?.Contains(token, StringComparison.OrdinalIgnoreCase) == true));
    }

    private static string Cell(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value;
    }
}
