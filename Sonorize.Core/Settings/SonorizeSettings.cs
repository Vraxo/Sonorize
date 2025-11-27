namespace Sonorize.Core.Settings;

public class SonorizeSettings
{
    public SonorizeTheme Theme { get; set; } = new();
    public PlaybackSettings Playback { get; set; } = new();
    public LibrarySettings Library { get; set; } = new();
    public WindowSettings Window { get; set; } = new();
    public LastfmSettings Lastfm { get; set; } = new();

    public void ApplyTheme(SonorizeTheme theme)
    {
        // Replace the current theme instance
        Theme = theme;
    }

    public SonorizeTheme ExtractTheme()
    {
        // Return a deep copy to avoid reference issues if the caller modifies it.
        return Theme.Clone();
    }
}