using Sonorize.Core.Helpers;

namespace Sonorize.Core.Services.Library;

public class DataRebuildScheduler : IDisposable
{
    private readonly ActionDebouncer _debouncer = new();
    private readonly Func<Task> _rebuildAction;

    public DataRebuildScheduler(Func<Task> rebuildAction)
    {
        _rebuildAction = rebuildAction;
    }

    public void ScheduleRebuild(bool immediate = false)
    {
        if (immediate)
        {
            _ = _rebuildAction();
            return;
        }

        _debouncer.Debounce(async () => await _rebuildAction(), 1000);
    }

    public void Dispose()
    {
        _debouncer.Dispose();
    }
}