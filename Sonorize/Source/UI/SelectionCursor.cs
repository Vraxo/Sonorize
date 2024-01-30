namespace Sonorize;

class SelectionCursor : Component
{
    // Fields

    public int Y = 0;

    // Public

    public void GoUp()
    {
        Y = program.GlobalState.State == State.ViewingPlaylists ?
            GoUpThePlaylists() :
            GoUpTheSongs();
    }

    public void GoDown()
    {
        Y = program.GlobalState.State == State.ViewingPlaylists ?
            GoDownThePlaylists() :
            GoDownTheSongs();
    }

    // Viewing playlists

    private int GoUpThePlaylists()
    {
        PlaylistsPrinter printer = program.PlaylistsPrinter;

        if (Y > 0)
        {
            return Y - 1;
        }
        else
        {
            printer.Page --;
        }

        return Y;
    }

    private int GoDownThePlaylists()
    {
        PlaylistCollection playlistsCollection = program.PlaylistCollection;
        PlaylistsPrinter printer = program.PlaylistsPrinter;
        int playlistsPerPage = printer.PlaylistsPerPage;

        int numberOfPlaylistsOnTheLastPage = playlistsCollection.Playlists.Count - 1 - printer.PlaylistsInPreviousPages;

        if (Y < numberOfPlaylistsOnTheLastPage)
        {
            if (Y == playlistsPerPage - 1)
            {
                printer.Page ++;
                return 0;
            }

            return Y + 1;
        }

        return Y;
    }

    // Viewing songs

    private int GoUpTheSongs()
    {
        SongsListPrinter printer = program.SongsListPrinter;

        if (Y > 0)
        {
            return Y - 1;
        }
        else
        {
            printer.Page --;
        }

        return Y;
    }

    private int GoDownTheSongs()
    {
        SongsList songsList = program.SongsList;
        SongsListPrinter printer = program.SongsListPrinter;
        int songsPerPage = printer.SongsPerPage;

        int numberOfSongsOnTheLastPage = songsList.Songs.Count - 1 - printer.SongsInPreviousPages;

        if (Y < numberOfSongsOnTheLastPage)
        {
            if (Y == songsPerPage - 1)
            {
                printer.Page++;
                return 0;
            }

            return Y + 1;
        }

        return Y;
    }
}