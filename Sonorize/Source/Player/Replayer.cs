namespace Sonorize;

class Replayer : Component
{
    // Fields

    public ReplayMode Mode = ReplayMode.Stop;

    // Public

    public void Toggle()
    {
        switch (Mode)
        {
            case ReplayMode.Stop:
                Mode = ReplayMode.Loop;
                break;

            case ReplayMode.Loop:
                Mode = ReplayMode.Cycle;
                break;

            case ReplayMode.Cycle:
                Mode = ReplayMode.Infinite;
                break;

            case ReplayMode.Infinite:
                Mode = ReplayMode.Shuffle;
                break;

            case ReplayMode.Shuffle:
                Mode = ReplayMode.Stop;
                break;
        }

        program.Settings.Save();
    }
}