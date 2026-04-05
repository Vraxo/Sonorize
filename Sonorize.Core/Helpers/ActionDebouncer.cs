namespace Sonorize.Core.Helpers;

public class ActionDebouncer : IDisposable
{
    private CancellationTokenSource? _cts;
    private readonly Lock _lock = new();

    public void Debounce(Action action, int milliseconds = 500)
    {
        lock (_lock)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            CancellationToken token = _cts.Token;

            Task.Delay(milliseconds, token).ContinueWith(t =>
            {
                if (t.IsCanceled)
                {
                    return;
                }

                action();
            }, TaskScheduler.Default);
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        GC.SuppressFinalize(this);
    }
}