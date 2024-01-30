namespace Sonorize;

class GlobalState : Component
{
    // Fields

    public State State = State.ViewingPlaylists;
    private string temporaryPlaylistPath = "Resources/Playlists/Temporary.txt";

    // Public

    public void Toggle()
    {
        if (State == State.ViewingPlaylists)
        {
            ToggleToViewingSongs();
        }
        else
        {
            if (program.SongsListPrinter.Page > 0)
            {
                program.SongsListPrinter.Page --;
            }
            else
            {
                State = State.ViewingPlaylists;
                program.PlaylistCollection.DeleteTemporaryPlaylist();
            }
        }

        program.SelectionCursor.Y = 0;
    }

    public void ToggleToSingleSong(string filePath)
    {
        State = State.ViewingSongs;

        Playlist temporaryPlaylist = new(temporaryPlaylistPath, new() { filePath });
        program.PlaylistCollection.Add(temporaryPlaylist);

        program.PlaylistCollection.CurrentPlaylist = temporaryPlaylist;

        program.SongsList.Load();

        program.Player.PlaySelected();
    }

    // Private

    private void ToggleToViewingSongs()
    {
        if (program.PlaylistCollection.Playlists.Count < 1)
        {
            return;
        }

        State = State.ViewingSongs;

        int cursor = program.SelectionCursor.Y;
        Playlist currentPlaylist = program.PlaylistCollection.Playlists[cursor];
        program.PlaylistCollection.CurrentPlaylist = currentPlaylist;

        program.SongsList.Load();
    }
}