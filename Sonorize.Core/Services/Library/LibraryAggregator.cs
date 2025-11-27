using Sonorize.Core.Models;

namespace Sonorize.Core.Services.Library;

public class LibraryAggregator
{
    public (List<AlbumGroup> Albums, List<ArtistGroup> Artists) Aggregate(IEnumerable<Song> songs)
    {
        // Snapshot to prevent enumeration issues if the source is modified elsewhere
        var songList = songs.ToList();

        // 1. Albums
        // Group by Album + Artist to handle same-named albums by different artists
        var albums = songList
            .GroupBy(s => new { s.Album, s.Artist })
            .Select(g => new AlbumGroup
            {
                Title = g.Key.Album,
                Artist = g.Key.Artist,
                SongCount = g.Count(),
                // Prefer a file with art, otherwise fallback to any file
                RepresentativeFilePath = g.FirstOrDefault(s => s.HasArt)?.FilePath ?? g.FirstOrDefault()?.FilePath
            })
            .OrderBy(a => a.Title)
            .ToList();

        // 2. Artists
        var artists = songList
            .GroupBy(s => s.Artist)
            .Select(g => new ArtistGroup
            {
                Name = g.Key,
                SongCount = g.Count(),
                AlbumCount = g.Select(s => s.Album).Distinct().Count(),
                RepresentativeFilePath = g.FirstOrDefault(s => s.HasArt)?.FilePath ?? g.FirstOrDefault()?.FilePath
            })
            .OrderBy(a => a.Name)
            .ToList();

        return (albums, artists);
    }
}