namespace Sonorize;

class PlaylistCollection : Component
{
    // Fields
    
    public List<Playlist> Playlists = new();
    public Playlist CurrentPlaylist;
    private readonly string playlistsPath = "Resources/Playlists";

    // Constructor

    public PlaylistCollection()
    {
        Load();
        DeleteTemporaryPlaylist();
    }

    // Public

    public void Add()
    {
        Console.Write("Playlist name: ");
        string name = Console.ReadLine();
        string filePath = $"Resources/Playlists/{name}.txt";

        if (Path.Exists(filePath))
        {
            return;
        }

        Playlist playlist = new(filePath, new());
        Add(playlist);
    }

    public void Add(Playlist playlist)
    {
        Playlists.Add(playlist);
        Save();
    }

    public void Delete()
    {
        if (Playlists.Count < 1)
        {
            return;
        }

        program.PlaylistUndeleter.Remember();

        File.Delete(Playlists[program.SelectionCursor.Y].FilePath);
        Playlists.RemoveAt(program.SelectionCursor.Y);
        Save();

        program.SelectionCursor.GoUp();
    }

    public void DeleteTemporaryPlaylist()
    {
        for (int i = 0; i < Playlists.Count; i ++) 
        {
            if (Playlists[i].Name == "Temporary")
            {
                Playlists.Remove(Playlists[i]);
                File.Delete("Resources/Playlists/Temporary.txt");
            }
        }
    }

    // Private

    private void Save()
    {
        foreach (Playlist playlist in Playlists)
        {
            File.WriteAllLines(playlist.FilePath, playlist.SongPaths);
        }
    }

    private void Load()
    {
        if (!Directory.Exists(playlistsPath))
        {
            return;
        }

        string[] playlistPaths = Directory.GetFiles(playlistsPath);

        foreach (string playlistPath in playlistPaths)
        {
            List<string> songPaths = File.ReadAllLines(playlistPath).ToList();
            Playlist playlist = new(playlistPath, songPaths);
            Playlists.Add(playlist);
        }

        Playlists = Playlists.OrderBy(o => o.Name).ToList();
    }
}