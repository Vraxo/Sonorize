namespace Sonorize;

class InstructionsPrinter : Component
{
    // Fields

    public bool IsEnabled = true;

    // Public

    public void Print()
    {
        if (!IsEnabled)
        {
            return;
        }

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("\n\n\n");
        Console.WriteLine("   - - - - - - - - - - - - - - -");
        Console.WriteLine("   N          - New playlist/song");
        Console.WriteLine("   L          - Change replay mode");
        Console.WriteLine("   +/-        - Increase/decrease volume");
        Console.WriteLine("   Escape     - Go back/exit");
        Console.WriteLine("   Arrow Keys - Movement");
        Console.WriteLine("   Backspace  - Delete playlist/song");
        Console.WriteLine("   Z          - Undelete playlist/song");
        Console.WriteLine("   H          - Hide/unhide these instructions in the future");
    }

    public void Toggle()
    {
        IsEnabled = !IsEnabled;
        program.Settings.Save();
    }
}