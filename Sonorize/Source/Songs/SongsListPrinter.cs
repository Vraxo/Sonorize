namespace Sonorize;

class SongsListPrinter : Component
{
    // Fields

    public readonly int SongsPerPage = 20;

    // Properties
    
    private int _page = 0;

    public int Page
    {
        get => _page;

        set
        {
            if (value > _page)
            {
                if (program.SongsList.Songs.Count > SongsInPreviousPages)
                {
                    _page = value;
                }
            }
            else
            {
                if (value >= 0)
                {
                    _page = value;
                    program.SelectionCursor.Y = SongsPerPage - 1;
                }
            }
        }
    }

    public int SongsInPreviousPages
    {
        get => Page * SongsPerPage;
    }

    // Public

    public void Print()
    {
        SongsList songsList = program.SongsList;

        for (int i = Page * SongsPerPage; i < Math.Min(SongsInPreviousPages + SongsPerPage, songsList.Songs.Count); i++)
        {
            PrintSong(songsList.Songs[i], i);
        }
    }

    // Private

    private void PrintSong(string songPath, int index)
    {
        string cursor = (program.SelectionCursor.Y == index - SongsInPreviousPages) ? "   > " : "     ";
        Console.ForegroundColor = (cursor == "   > ") ? program.Theme.Selected : program.Theme.Default;

        string name = Path.GetFileNameWithoutExtension(songPath);

        Console.WriteLine($"{cursor}{name}");

        Console.ForegroundColor = program.Theme.Default;
    }
}