namespace Sonorize;

class InputHandler : Component
{
    // Public

    public void Update()
    {
        ConsoleKeyInfo key = Console.ReadKey(true);
        HandleKeyPress(key);
    }

    // Private

    private void HandleKeyPress(ConsoleKeyInfo key)
    {
        SharedInput(key);

        switch (program.GlobalState.State)
        {
            case State.ViewingSongs:
                ViewingSongs(key);
                break;

            case State.ViewingPlaylists:
                ViewingPlaylists(key); 
                break;
        }
    }

    private void ViewingSongs(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.Escape:
                program.GlobalState.Toggle();
                break;

            case ConsoleKey.Enter:
                program.Player.PlaySelected();
                break;

            case ConsoleKey.N:
                program.SongsList.AddFromFile();
                break;

            case ConsoleKey.Backspace:
                program.SongsList.Delete();
                break;

            case ConsoleKey.UpArrow:
                program.SelectionCursor.GoUp();
                break;

            case ConsoleKey.DownArrow:
                program.SelectionCursor.GoDown();
                break;

            case ConsoleKey.Z:
                program.SongUndeleter.Revert();
                break;

            case ConsoleKey.RightArrow:
                program.SongsListPrinter.Page ++;
                break;

            case ConsoleKey.LeftArrow:
                program.SongsListPrinter.Page --;
                break;
        }
    }

    private void ViewingPlaylists(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.N:
                program.NewPlaylistPrompter.IsMakingNewPlaylist = true;
                break;

            case ConsoleKey.DownArrow:
                program.SelectionCursor.GoDown();
                break;

            case ConsoleKey.UpArrow:
                program.SelectionCursor.GoUp();
                break;

            case ConsoleKey.Enter:
                program.GlobalState.Toggle();
                break;

            case ConsoleKey.Escape:
                program.IsStopping = true;
                break;

            case ConsoleKey.Backspace:
                program.PlaylistCollection.Delete();
                break;

            case ConsoleKey.Z:
                program.PlaylistUndeleter.Revert();
                break;
        }
    }

    private void SharedInput(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.OemPlus:
                program.VolumeController.Increase();
                break;

            case ConsoleKey.OemMinus:
                program.VolumeController.Decrease();
                break;

            case ConsoleKey.L:
                program.Replayer.Toggle();
                break;

            case ConsoleKey.Spacebar:
                program.Player.Pause();
                break;

            case ConsoleKey.H:
                program.InstructionsPrinter.Toggle();
                break;
        }
    }
}