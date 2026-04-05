using Sonorize.Core.Models;

namespace Sonorize.Core.Services.Library;

public class LibraryAggregator
{
    public (List<AlbumGroup> Albums, List<ArtistGroup> Artists) Aggregate(IEnumerable<Song> songs)
    {
        List<Song> songList = [.. songs];

        List<AlbumGroup> albums = GetAlbums(songList);
        List<ArtistGroup> artists = GetArtists(songList);

        return (albums, artists);
    }

    private static List<AlbumGroup> GetAlbums(List<Song> songList)
    {
        // 1. Albums
        // Group by Album + Artist to handle same-named albums by different artists
        List<AlbumGroup> albums = [.. songList
            .GroupBy(s => new { s.Album, s.Artist })
            .Select(g => new AlbumGroup
            {
                Title = g.Key.Album,
                Artist = g.Key.Artist,
                SongCount = g.Count(),
                // Prefer a file with art, otherwise fallback to any file
                RepresentativeFilePath = g.FirstOrDefault(s => s.HasArt)?.FilePath ?? g.FirstOrDefault()?.FilePath
            })
            .OrderBy(a => a.Title)];

        return albums;
    }

    private static List<ArtistGroup> GetArtists(List<Song> songList)
    {
        return [.. songList
            .GroupBy(s => s.Artist)
            .Select(g => new ArtistGroup
            {
                Name = g.Key,
                SongCount = g.Count(),
                AlbumCount = g.Select(s => s.Album).Distinct().Count(),
                RepresentativeFilePath = g.FirstOrDefault(s => s.HasArt)?.FilePath ?? g.FirstOrDefault()?.FilePath
            })
            .OrderBy(a => a.Name)];
    }
}