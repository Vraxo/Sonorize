namespace Sonorize;

class SongUndeleter : Component
{
    // Fields

    private Stack<string> deletedSongPaths = new Stack<string>();

    // Public

    public void Remember()
    {
        if (program.SongsList.Songs.Count > 0)
        {
            string deletedSongPath = program.SongsList.Songs[program.SelectionCursor.Y];
            deletedSongPaths.Push(deletedSongPath);
        }
    }

    public void Revert()
    {
        if (deletedSongPaths.Count > 0)
        {
            string lastDeletedSong = deletedSongPaths.Pop();
            program.SongsList.AddFromPath(lastDeletedSong);
        }
    }
}