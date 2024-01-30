namespace Sonorize;

class PlaylistUndeleter : Component
{
    // Fields

    private Stack<Playlist> deletedPlaylists = new();

    // Public

    public void Remember()
    {
        if (program.PlaylistCollection.Playlists.Count > 0)
        {
            int cursor = program.SelectionCursor.Y;
            Playlist playlist = program.PlaylistCollection.Playlists[cursor];
            deletedPlaylists.Push(playlist);
        }
    }

    public void Revert()
    {
        if (deletedPlaylists.Count > 0)
        {
            Playlist lastDeletedPlaylist = deletedPlaylists.Pop();
            program.PlaylistCollection.Add(lastDeletedPlaylist);
        }
    }
}