using Terminal.Gui.Drawing;
using Attribute = Terminal.Gui.Drawing.Attribute;

namespace Sonorize.Tui;

/// <summary>
/// The colours the TUI uses, all from the terminal's own 16-colour palette.
/// </summary>
/// <remarks>
/// Program enables Terminal.Gui's 16-colour mode, so "Green" here is emitted as
/// the ANSI code for green and shows as whatever green the user's terminal theme
/// defines. <see cref="Color.None"/> means "the terminal's default", which keeps
/// the terminal's own background (including transparency).
/// </remarks>
internal static class TuiColors
{
    private const ColorName16 AccentColor = ColorName16.Cyan;
    private const ColorName16 PlayingColor = ColorName16.Green;
    private const ColorName16 ErrorColor = ColorName16.Red;
    private const ColorName16 NoticeColor = ColorName16.Yellow;

    // Most terminal themes draw magenta as purple.
    private const ColorName16 HighlightColor = ColorName16.Magenta;

    /// <summary>Headings: table header, search label, progress bar.</summary>
    public static readonly Scheme Accent = new(OnDefault(AccentColor));

    /// <summary>The playing track, in the list and in the now-playing bar.</summary>
    public static readonly Scheme Playing = new()
    {
        Normal = OnDefault(PlayingColor),

        // Selected row: invert, so the selection stays visible on the playing track.
        Focus = new Attribute(ColorName16.Black, PlayingColor),
        Active = new Attribute(ColorName16.Black, PlayingColor)
    };

    /// <summary>
    /// Secondary text. Uses the terminal's "faint" style on the default colour
    /// rather than dark gray, which some themes render close to the background.
    /// </summary>
    public static readonly Scheme Muted = new(new Attribute(Color.None, Color.None, TextStyle.Faint));

    public static readonly Scheme Error = new(OnDefault(ErrorColor));

    /// <summary>States worth noticing: paused, shuffle on, repeat on.</summary>
    public static readonly Scheme Notice = new(OnDefault(NoticeColor));

    /// <summary>Border and title of the pane that has keyboard focus.</summary>
    public static readonly Scheme FocusedBorder = new()
    {
        Normal = OnDefault(HighlightColor),
        HotNormal = OnDefault(HighlightColor),
        Focus = OnDefault(HighlightColor),
        HotFocus = OnDefault(HighlightColor)
    };

    /// <summary>
    /// Artist names in a list row. Only the unselected look changes, so the
    /// selection bar stays one solid colour across the row.
    /// </summary>
    public static Scheme ArtistCell(Scheme row)
    {
        return new Scheme
        {
            Normal = OnDefault(HighlightColor),
            Focus = row.Focus,
            Active = row.Active
        };
    }

    private static Attribute OnDefault(ColorName16 foreground)
    {
        return new Attribute(foreground, Color.None);
    }
}
