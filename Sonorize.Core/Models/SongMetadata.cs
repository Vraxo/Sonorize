namespace Sonorize.Core.Models;

public class SongMetadata
{
    public string Title { get; set; } = "";
    public string Artist { get; set; } = "";
    public string Album { get; set; } = "";
    public string AlbumArtists { get; set; } = "";
    public string Genre { get; set; } = "";
    public uint Year { get; set; }
    public uint Track { get; set; }
    public uint Disc { get; set; }
}