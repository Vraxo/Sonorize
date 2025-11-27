using Sonorize.Core.Models;

namespace Sonorize.Core.Helpers;

public static class DemoDataGenerator
{
    public static List<Song> Generate(int count = 50)
    {
        var songs = new List<Song>();
        var random = new Random(42); // Fixed seed for consistent screenshots

        var artists = new[]
        {
            ("The Midnight", "Synthwave"),
            ("Daft Punk", "Electronic"),
            ("Pink Floyd", "Rock"),
            ("Kendrick Lamar", "Hip Hop"),
            ("Tame Impala", "Psychedelic"),
            ("Hans Zimmer", "Score"),
            ("Norah Jones", "Jazz"),
            ("Foo Fighters", "Rock")
        };

        var albums = new Dictionary<string, string[]>
        {
            { "The Midnight", ["Endless Summer", "Nocturnal", "Monsters"] },
            { "Daft Punk", ["Discovery", "Random Access Memories", "Homework"] },
            { "Pink Floyd", ["The Dark Side of the Moon", "The Wall", "Animals"] },
            { "Kendrick Lamar", ["DAMN.", "To Pimp a Butterfly", "good kid, m.A.A.d city"] },
            { "Tame Impala", ["Currents", "The Slow Rush", "Innerspeaker"] },
            { "Hans Zimmer", ["Inception", "Interstellar", "Dune"] },
            { "Norah Jones", ["Come Away With Me", "Feels Like Home", "Day Breaks"] },
            { "Foo Fighters", ["The Colour and the Shape", "Wasting Light", "Echoes, Silence, Patience & Grace"] }
        };

        var titles = new[]
        {
            "Midnight City", "Sunset Drive", "Neon Lights", "Deep Space", "Lost in Time",
            "Echoes of Yesterday", "Future Club", "Digital Love", "Harder Better Faster",
            "Time", "Money", "Us and Them", "DNA", "Humble", "Let It Happen", "The Less I Know",
            "Dreaming", "Sunrise", "Don't Know Why", "Everlong", "My Hero", "Walk"
        };

        for (int i = 0; i < count; i++)
        {
            var (artist, _) = artists[random.Next(artists.Length)];
            var artistAlbums = albums[artist];
            var album = artistAlbums[random.Next(artistAlbums.Length)];
            var title = titles[random.Next(titles.Length)] + (i > 20 ? $" {i}" : ""); // Ensure unique-ish titles

            songs.Add(new Song
            {
                // Use a special URI scheme we can detect later
                FilePath = $"demo://{artist}/{album}/{title}.mp3",
                Title = title,
                Artist = artist,
                Album = album,
                Duration = TimeSpan.FromSeconds(random.Next(180, 400)),
                HasArt = true // Force UI to request art
            });
        }

        return songs.OrderBy(s => s.Artist).ThenBy(s => s.Album).ThenBy(s => s.Title).ToList();
    }

    public static List<Playlist> GeneratePlaylists(List<Song> songs)
    {
        var playlists = new List<Playlist>();
        var random = new Random(42);

        // Removed "Discover Weekly" to avoid implying algorithmic generation features
        var names = new[] { "Late Night Drive", "Coding Focus", "Workout", "Chill Vibes", "Favorites" };

        foreach (var name in names)
        {
            // Pick 10-20 random songs
            var playlistSongs = songs.OrderBy(x => random.Next()).Take(random.Next(10, 20)).Select(s => s.FilePath).ToList();

            playlists.Add(new Playlist
            {
                Id = Guid.NewGuid(),
                Name = name,
                Type = PlaylistType.Manual,
                SongFilePaths = playlistSongs
            });
        }

        return playlists;
    }
}