using Sonorize.Core.Models;

namespace Sonorize.Core.Helpers;

public static class DemoDataGenerator
{
    public const string DemoScheme = "demo://";

    private const int UniqueTitleThreshold = 20;
    private const int DurationMinSeconds = 180;
    private const int DurationMaxSeconds = 400;
    private const int PlaylistMinSize = 10;
    private const int PlaylistMaxSize = 20;
    private const int DefaultSongCount = 50;
    private const int DeterministicSeed = 42;

    private static readonly (string Name, string Genre)[] Artists =
    [
        ("The Midnight", "Synthwave"),
        ("Daft Punk", "Electronic"),
        ("Pink Floyd", "Rock"),
        ("Kendrick Lamar", "Hip Hop"),
        ("Tame Impala", "Psychedelic"),
        ("Hans Zimmer", "Score"),
        ("Norah Jones", "Jazz"),
        ("Foo Fighters", "Rock")
    ];

    private static readonly Dictionary<string, string[]> AlbumsByArtist = new()
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

    private static readonly string[] SongTitles =
    [
        "Midnight City", "Sunset Drive", "Neon Lights", "Deep Space", "Lost in Time",
        "Echoes of Yesterday", "Future Club", "Digital Love", "Harder Better Faster",
        "Time", "Money", "Us and Them", "DNA", "Humble", "Let It Happen", "The Less I Know",
        "Dreaming", "Sunrise", "Don't Know Why", "Everlong", "My Hero", "Walk"
    ];

    private static readonly string[] PlaylistNames =
    [
        "Late Night Drive", "Coding Focus", "Workout", "Chill Vibes", "Favorites"
    ];

    public static List<Song> Generate(int count = DefaultSongCount)
    {
        Random rng = new(DeterministicSeed);
        List<Song> songs = new(count);

        for (int i = 0; i < count; i++)
        {
            (string? artist, string _) = Artists[rng.Next(Artists.Length)];
            string album = SelectAlbum(artist, rng);
            string title = BuildTitle(i, rng);

            songs.Add(CreateSong(artist, album, title, rng));
        }

        return [.. songs
            .OrderBy(s => s.Artist)
            .ThenBy(s => s.Album)
            .ThenBy(s => s.Title)];
    }

    public static List<Playlist> GeneratePlaylists(List<Song> songs)
    {
        Random rng = new(DeterministicSeed);
        List<Playlist> playlists = [];

        foreach (string name in PlaylistNames)
        {
            List<Song> selectedSongs = SelectRandomSongs(songs, rng);
            playlists.Add(CreatePlaylist(name, selectedSongs));
        }

        return playlists;
    }

    private static string SelectAlbum(string artist, Random rng)
    {
        return AlbumsByArtist[artist][rng.Next(AlbumsByArtist[artist].Length)];
    }

    private static string BuildTitle(int index, Random rng)
    {
        string baseTitle = SongTitles[rng.Next(SongTitles.Length)];
        return index > UniqueTitleThreshold ? $"{baseTitle} {index}" : baseTitle;
    }

    private static Song CreateSong(string artist, string album, string title, Random rng)
    {
        return new()
        {
            FilePath = $"{DemoScheme}{artist}/{album}/{title}.mp3",
            Title = title,
            Artist = artist,
            Album = album,
            Duration = GenerateDuration(rng),
            HasArt = true
        };
    }

    private static TimeSpan GenerateDuration(Random rng)
    {
        return TimeSpan.FromSeconds(rng.Next(DurationMinSeconds, DurationMaxSeconds));
    }

    private static List<Song> SelectRandomSongs(List<Song> songs, Random rng)
    {
        int size = rng.Next(PlaylistMinSize, PlaylistMaxSize);
        return songs.OrderBy(_ => rng.Next()).Take(size).ToList();
    }

    private static Playlist CreatePlaylist(string name, List<Song> songs)
    {
        return new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = PlaylistType.Manual,
            SongFilePaths = songs.Select(s => s.FilePath).ToList()
        };
    }
}