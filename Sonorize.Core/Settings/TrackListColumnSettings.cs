namespace Sonorize.Core.Settings;

public class TrackListColumnSettings
{
    public bool ShowIndex { get; set; } = true;
    public bool ShowArt { get; set; } = false; // NEW
    public bool ShowArtist { get; set; } = true;
    public bool ShowAlbum { get; set; } = true;
    public bool ShowDuration { get; set; } = true;
}