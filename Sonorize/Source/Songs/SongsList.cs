namespace Sonorize;

class SongsList : Component
{
    // Fields

    public List<string> Songs = new();

    // Public

    public void AddFromFile()
    {
        OpenFileDialog openFileDialog = new();
        openFileDialog.Multiselect = true;
        openFileDialog.ShowDialog();

        string[] filePaths = openFileDialog.FileNames;

        foreach (string filePath in filePaths)
        {
            if (!Songs.Contains(filePath))
            {
                if (Path.GetExtension(filePath) == ".mp3")
                {
                    Add(filePath);
                }
            }
        }

        Save();
    }

    public void AddFromPath(string filePath)
    {
        Add(filePath);
        Save();
    }

    public void Load()
    {
        Songs.Clear();

        string currentPlaylistPath = program.PlaylistCollection.CurrentPlaylist.FilePath;
        string[] paths = File.ReadAllLines(currentPlaylistPath);

        foreach (string music in paths)
        {
            Songs.Add(music);
        }
    }

    public void Delete()
    {
        if (Songs.Count < 1)
        {
            return;
        }

        program.SongUndeleter.Remember();
        Songs.RemoveAt(program.SelectionCursor.Y);
        Save();

        program.SelectionCursor.GoUp();
    }

    // Private

    private void Add(string filePath)
    {
        Songs.Add(filePath);
        program.PlaylistCollection.CurrentPlaylist.SongPaths.Add(filePath);
    }

    private void Save()
    {
        Songs.Sort();
        string currentPlaylistPath = program.PlaylistCollection.CurrentPlaylist.FilePath;
        File.WriteAllLines(currentPlaylistPath, Songs);
    }
}