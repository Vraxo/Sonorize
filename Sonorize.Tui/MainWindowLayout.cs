using System.Collections.ObjectModel;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Sonorize.Tui;

/// <summary>
/// Builds the view tree of the main window: search box, sidebar, cover art,
/// track table and the now-playing bar. Owns the layout constants and the
/// focus-border colouring; the controllers own what each view shows.
/// </summary>
internal sealed class MainWindowLayout
{
    // Wide enough for a roughly square cover: terminal cells are about twice as tall as wide.
    internal const int SidebarWidth = 22;

    // Four entries plus the border.
    internal const int SidebarListHeight = 6;
    internal const int FooterHeight = 4;

    // "999:59 / 999:59"
    internal const int TimeLabelWidth = 15;

    // "Shuffle off" and "Repeat off", the longest texts of each label.
    internal const int ShuffleLabelWidth = 11;
    internal const int RepeatLabelWidth = 10;
    internal const int StatusGap = 3;

    public TextField Search { get; }
    public ListView Sidebar { get; }
    public TableView Table { get; }
    public ImageView Cover { get; }
    public Label EmptyHint { get; }
    public Label NowPlaying { get; }
    public ProgressBar Progress { get; }
    public Label Time { get; }
    public Label Status { get; }
    public Label Shuffle { get; }
    public Label Repeat { get; }

    /// <summary>Every view, in the order they are added to the window.</summary>
    public IReadOnlyList<View> Views { get; }

    private bool _sidebarBorderFocused;
    private bool _tableBorderFocused;

    public MainWindowLayout()
    {
        Label searchLabel = new() { Text = " Search: ", X = 0, Y = 0 };

        Search = new TextField
        {
            X = Pos.Right(searchLabel),
            Y = 0,
            Width = Dim.Fill(1)
        };

        Sidebar = new ListView
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
        Sidebar.SetSource(new ObservableCollection<string>(Enum.GetNames<BrowseMode>()));

        // Draws real pixels on terminals with Kitty graphics or Sixel, and
        // coloured cells everywhere else.
        Cover = new ImageView
        {
            Title = "Cover",
            BorderStyle = LineStyle.Rounded,
            X = 0,
            Y = Pos.Bottom(Sidebar),
            Width = SidebarWidth,
            Height = Dim.Fill(FooterHeight),
            CanFocus = false
        };

        Table = new TableView
        {
            Title = "Songs",
            BorderStyle = LineStyle.Rounded,
            X = Pos.Right(Sidebar),
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill(FooterHeight),
            FullRowSelect = true,
            MultiSelect = false,
            CollectionNavigator = null
        };
        Table.Style.ShowHorizontalHeaderOverline = false;
        Table.Style.ShowVerticalCellLines = false;
        Table.Style.ShowVerticalHeaderLines = false;
        Table.Style.AlwaysShowHeaders = true;
        Table.Style.HeaderScheme = TuiColors.Accent;

        EmptyHint = new Label
        {
            X = Pos.Center(),
            Y = Pos.Center(),
            Visible = false
        };

        NowPlaying = new Label
        {
            X = 1,
            Y = Pos.AnchorEnd(FooterHeight),
            Width = Dim.Fill(1),
            Height = 1
        };

        Time = new Label
        {
            X = Pos.AnchorEnd(TimeLabelWidth + 1),
            Y = Pos.AnchorEnd(FooterHeight - 1),
            Width = TimeLabelWidth,
            Height = 1
        };

        Progress = new ProgressBar
        {
            X = 1,
            Y = Pos.AnchorEnd(FooterHeight - 1),
            Width = Dim.Fill(TimeLabelWidth + 3),
            Height = 1
        };

        Shuffle = new Label
        {
            X = 1,
            Y = Pos.AnchorEnd(FooterHeight - 2),
            Width = ShuffleLabelWidth,
            Height = 1
        };

        Repeat = new Label
        {
            X = Pos.Right(Shuffle) + StatusGap,
            Y = Pos.AnchorEnd(FooterHeight - 2),
            Width = RepeatLabelWidth,
            Height = 1
        };

        Status = new Label
        {
            X = Pos.Right(Repeat) + StatusGap,
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
        EmptyHint.SetScheme(TuiColors.Muted);
        NowPlaying.SetScheme(TuiColors.Playing);
        Progress.SetScheme(TuiColors.Accent);
        Time.SetScheme(TuiColors.Muted);
        Status.SetScheme(TuiColors.Muted);
        hints.SetScheme(TuiColors.Muted);

        Views = [searchLabel, Search, Sidebar, Cover, Table, EmptyHint, NowPlaying, Progress, Time, Shuffle, Repeat, Status, hints];
    }

    /// <summary>Colours the border of whichever pane has keyboard focus.</summary>
    public void UpdateFocusBorders()
    {
        bool sidebarFocused = Sidebar.HasFocus;
        bool tableFocused = Table.HasFocus;

        if (sidebarFocused != _sidebarBorderFocused)
        {
            _sidebarBorderFocused = sidebarFocused;
            Sidebar.Border.GetOrCreateView().SetScheme(sidebarFocused ? TuiColors.FocusedBorder : null);
        }

        if (tableFocused != _tableBorderFocused)
        {
            _tableBorderFocused = tableFocused;
            Table.Border.GetOrCreateView().SetScheme(tableFocused ? TuiColors.FocusedBorder : null);
        }
    }
}
