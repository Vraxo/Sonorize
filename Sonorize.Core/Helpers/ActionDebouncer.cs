namespace Sonorize.Core.Helpers;

public class ActionDebouncer : IDisposable
{
    private CancellationTokenSource? _cts;
    private readonly object _lock = new();

    public void Debounce(Action action, int milliseconds = 500)
    {
        lock (_lock)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            _ = Task.Delay(milliseconds, token).ContinueWith(t =>
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