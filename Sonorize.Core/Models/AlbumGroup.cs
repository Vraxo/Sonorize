namespace Sonorize.Core.Models;

public class AlbumGroup
{
    public required string Title { get; set; }
    public required string Artist { get; set; }
    public string? RepresentativeFilePath { get; set; }
    public int SongCount { get; set; }
}