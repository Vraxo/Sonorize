using Sonorize.Core.Core;
using Sonorize.Core.Library;
using Sonorize.Core.Playback;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Sonorize.Tui;

/// <summary>Renders browse results in the table, including column sizing and empty-state hints.</summary>
internal sealed class BrowseTablePresenter
{
    private readonly TableView _table;
    private readonly Label _emptyHint;
    private readonly IPlayerService _player;
    private BrowseResults? _results;
    private int _lastTableWidth = -1;

    public BrowseTablePresenter(TableView table, Label emptyHint, IPlayerService player)
    {
        _table = table;
        _emptyHint = emptyHint;
        _player = player;
        _table.Style.RowColorGetter = args => IsPlayingRow(args.RowIndex) ? TuiColors.Playing : null;
        _table.ViewportChanged += OnTableViewportChanged;
    }

    public int SelectedRow => _table.Value?.SelectedCell.Y ?? -1;

    private bool IsPlayingRow(int row) => _results is { ShowsSongs: true } results
        && row >= 0 && row < results.Songs.Count
        && results.Songs[row].FilePath == _player.CurrentSong?.FilePath;

    public void Present(BrowseResults results, bool keepSelection)
    {
        int previousRow = SelectedRow;
        int previousOffset = _table.RowOffset;
        _results = results;
        ITableSource source = results.DrillDown is null && results.Mode == BrowseMode.Artists ? BuildArtistSource()
            : results.DrillDown is null && results.Mode == BrowseMode.Albums ? BuildAlbumSource() : BuildSongSource();
        ApplyColumnWidths();
        _table.Table = source;
        _table.Title = $"{results.Title} ({source.Rows})";
        if (keepSelection && source.Rows > 0 && previousRow >= 0)
        {
            _table.RowOffset = Math.Min(previousOffset, source.Rows - 1);
            _table.SetSelection(0, Math.Min(previousRow, source.Rows - 1), false);
            _table.Update();
        }
        UpdateEmptyHint(source.Rows, results.Query);
    }

    private EnumerableTableSource<Song> BuildSongSource()
    {
        return new EnumerableTableSource<Song>(
            _results!.Songs,
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
            _results!.Artists,
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
            _results!.Albums,
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

        if (_results!.ShowsSongs)
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
        else if (_results!.Mode == BrowseMode.Artists)
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
        else if (_results!.IsQueueView)
        {
            _emptyHint.Text = "The queue is empty. Press Enter on a song to start playing.";
        }
        else if (_results!.LibraryIsEmpty)
        {
            _emptyHint.Text = "Your library is empty. Press a to add a music folder.";
        }
        else
        {
            _emptyHint.Text = "Nothing here.";
        }

        _emptyHint.Visible = true;
    }

    private void OnTableViewportChanged(object? sender, DrawEventArgs e)
    {
        if (_results is null || _table.Viewport.Width == _lastTableWidth)
            return;
        ApplyColumnWidths();
        _table.Update();
    }

    private static string Cell(string? value) => BrowseText.Cell(value);
}
