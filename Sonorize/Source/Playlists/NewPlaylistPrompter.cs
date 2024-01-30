namespace Sonorize;

class NewPlaylistPrompter : Component
{
    // Fields

    public bool IsMakingNewPlaylist = false;

    // Public

    public void Prompt()
    {
        if (!IsMakingNewPlaylist)
        {
            return;
        }

        IsMakingNewPlaylist = false;

        string filePath = GetFilePath();

        if (Path.Exists(filePath) || filePath == null)
        {
            return;
        }

        Playlist playlist = new(filePath, new());
        program.PlaylistCollection.Add(playlist);
    }

    // Private

    private string GetFilePath()
    {
        Console.WriteLine("   - - - - - - - - - - - - - - -");
        Console.Write("   Playlist name: ");

        string name = Console.ReadLine();
        string trimmedName = name.Trim();

        if (trimmedName.Length != 0)
        {
            string filePath = $"Resources/Playlists/{trimmedName}.txt";
            return filePath;
        }
        else
        {
            return null;
        }
    }
}