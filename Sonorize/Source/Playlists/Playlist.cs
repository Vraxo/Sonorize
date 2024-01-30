namespace Sonorize;

class Playlist
{
    // Fields

    public string FilePath;
    public List<string> SongPaths;

    // Properties

    public string Name
    {
        get
        {
            return Path.GetFileNameWithoutExtension(FilePath);
        }
    }

    // Constructor

    public Playlist(string filePath, List<string> songPaths)
    {
        FilePath = filePath;
        SongPaths = songPaths;
    }
}