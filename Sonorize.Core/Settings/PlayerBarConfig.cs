namespace Sonorize.Core.Settings;

public class PlayerBarConfig
{
    public List<PlayerBarWidget> Left { get; set; } = [PlayerBarWidget.Info];
    public List<PlayerBarWidget> Center { get; set; } = [PlayerBarWidget.StackedTransport];
    public List<PlayerBarWidget> Right { get; set; } =
    [
        PlayerBarWidget.Focus,
        PlayerBarWidget.SpeedPitch,
        PlayerBarWidget.Equalizer,
        PlayerBarWidget.Queue,
        PlayerBarWidget.Volume
    ];

    public PlayerBarConfig Clone()
    {
        return new PlayerBarConfig
        {
            Left = [.. Left],
            Center = [.. Center],
            Right = [.. Right]
        };
    }
}