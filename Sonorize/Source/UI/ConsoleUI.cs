namespace Sonorize;

class ConsoleUI : Component
{
    // Constructor

    public ConsoleUI()
    {
        Console.Title = "Sonorize";
        Console.WindowWidth = 65;
        Console.WindowHeight = 40;
        Console.CursorVisible = false;
    }

    // Public

    public void Update()
    {
        Console.Clear();

        switch (program.GlobalState.State)
        {
            case State.ViewingSongs:
                ViewingSongs();
                break;

            case State.ViewingPlaylists:
                ViewingPlaylists();
                break;
        }
    }

    // Private

    private void ViewingSongs()
    {
        program.SongNamePrinter.Print();
        program.SettingsPrinter.Print();
        program.PlaylistNamePrinter.Print();
        program.SongsListPrinter.Print();
        program.InstructionsPrinter.Print();
    }

    private void ViewingPlaylists()
    {
        program.SongNamePrinter.Print();
        program.SettingsPrinter.Print();
        program.PlaylistsPrinter.Print();
        program.NewPlaylistPrompter.Prompt();
        program.InstructionsPrinter.Print();
    }
}