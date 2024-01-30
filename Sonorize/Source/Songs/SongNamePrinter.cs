using NAudio.Wave;

namespace Sonorize;

class SongNamePrinter : Component
{
    // Public

    public void Print()
    {
        Console.ForegroundColor = program.Theme.Default;
        Console.Write(" * Now Playing: ");
        
        Console.ForegroundColor = GetSongNameColor();
        Console.Write($"\"{program.Player.SongName}\"\n");

        Console.ForegroundColor = program.Theme.Default;
        Console.WriteLine("   - - - - - - - - - - - - - - -");
    }

    // Private

    private ConsoleColor GetSongNameColor()
    {
        switch (program.Player.WaveOutDevice.PlaybackState)
        {
            case PlaybackState.Playing:
                return program.Theme.SongPlaying;
                
            case PlaybackState.Paused:
                return program.Theme.SongPaused;

            default:
                return program.Theme.SongStopped;
        }
    }
}