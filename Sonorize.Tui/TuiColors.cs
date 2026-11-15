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

    private static Attribute OnDefault(ColorName16 foreground)
    {
        return new Attribute(foreground, Color.None);
    }
}
