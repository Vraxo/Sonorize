namespace Sonorize.Core.Settings;

public class SonorizeSettings
{
    public SonorizeTheme Theme { get; set; } = new();
    public PlaybackSettings Playback { get; set; } = new();
    public LibrarySettings Library { get; set; } = new();
    public WindowSettings Window { get; set; } = new();
    public LastfmSettings Lastfm { get; set; } = new();
    public UpdateSettings Updates { get; set; } = new(); // NEW

    public void ApplyTheme(SonorizeTheme theme)
    {
        Theme = theme;
    }

    public SonorizeTheme ExtractTheme()
    {
        return Theme.Clone();
    }
}