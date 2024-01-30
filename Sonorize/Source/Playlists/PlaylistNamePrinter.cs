namespace Sonorize;

class PlaylistNamePrinter : Component
{
    // Fields

    private SongsList songsList;
    private SongsListPrinter songsListPrinter;

    // Public

    public void Print()
    {
        songsList = program.SongsList;
        songsListPrinter = program.SongsListPrinter;

        string name = program.PlaylistCollection.CurrentPlaylist.Name;
        int currentPage = songsListPrinter.Page + 1;
        int totalPages = (int)MathF.Ceiling((float)songsList.Songs.Count / songsListPrinter.SongsPerPage);

        totalPages = totalPages == 0 ? 1 : totalPages;

        Console.ForegroundColor = program.Theme.Playlists;
        Console.WriteLine($"   {name} - Page: {currentPage}/{totalPages}");
        Console.WriteLine("   - - - - - - - - - - - - - - -");
        Console.ForegroundColor = program.Theme.Default;
    }
}