namespace Sonorize;

class SettingsPrinter : Component
{
    // Public

    public void Print()
    {
        Console.ForegroundColor = program.Theme.Volume;
        Console.Write($"   [Volume: {program.VolumeController.Volume}%]");

        Console.ForegroundColor = program.Theme.Default;
        Console.Write(" | ");

        Console.ForegroundColor = program.Theme.Replay;
        Console.WriteLine($"[Replay: {program.Replayer.Mode}]");

        Console.ForegroundColor = program.Theme.Default;
        Console.WriteLine("   - - - - - - - - - - - - - - -");
    }
}