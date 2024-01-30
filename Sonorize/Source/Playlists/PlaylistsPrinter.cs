namespace Sonorize;

class PlaylistsPrinter : Component
{
    // Fields

    public readonly int PlaylistsPerPage = 20;

    // Properties

    private int _page = 0;

    public int Page
    {
        get => _page;

        set
        {
            if (value > _page)
            {
                if (program.PlaylistCollection.Playlists.Count > PlaylistsInPreviousPages)
                {
                    _page = value;
                }
            }
            else
            {
                if (value >= 0)
                {
                    _page = value;
                    program.SelectionCursor.Y = PlaylistsPerPage - 1;
                }
            }
        }
    }

    public int PlaylistsInPreviousPages
    {
        get => Page * PlaylistsPerPage;
    }

    // Public

    public void Print()
    {
        PrintTitle();

        List<Playlist> playlists = program.PlaylistCollection.Playlists;

        for (int i = Page * PlaylistsPerPage; i < Math.Min(PlaylistsInPreviousPages + PlaylistsPerPage, playlists.Count); i++)
        {
            PrintPlaylist(playlists[i], i);
        }
    }

    // Private

    private void PrintPlaylist(Playlist playlist, int index)
    {
        string cursor = (program.SelectionCursor.Y == index - PlaylistsInPreviousPages) ? "   > " : "     ";
        Console.ForegroundColor = (cursor == "   > ") ? program.Theme.Selected : program.Theme.Default;

        Console.WriteLine($"{cursor}{playlist.Name}");

        Console.ForegroundColor = program.Theme.Default;
    }

    private void PrintTitle()
    {
        PlaylistCollection playlistCollection = program.PlaylistCollection;

        int currentPage = program.PlaylistsPrinter.Page + 1;
        int totalPages = (int)MathF.Ceiling((float)playlistCollection.Playlists.Count / PlaylistsPerPage);
        totalPages = totalPages == 0 ? 1 : totalPages;

        Console.ForegroundColor = program.Theme.Playlists;
        Console.WriteLine($"   Playlists - Page: {currentPage}/{totalPages}");
        Console.WriteLine("   - - - - - - - - - - - - - - -");
        Console.ForegroundColor = program.Theme.Default;
    }
}