namespace Sonorize.Core.Library;

public class ArtistGroup
{
    public required string Name { get; set; }
    public int AlbumCount { get; set; }
    public int SongCount { get; set; }
    public string? RepresentativeFilePath { get; set; }
}
