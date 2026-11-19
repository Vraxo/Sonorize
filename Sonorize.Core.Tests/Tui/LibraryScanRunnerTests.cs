using Microsoft.Extensions.Logging.Abstractions;
using Sonorize.Core.Settings;
using Sonorize.Tui;

namespace Sonorize.Core.Tests.Tui;

public class LibraryScanRunnerTests
{
    [Fact]
    public async Task Concurrent_scans_stay_active_until_both_complete_and_each_marks_dirty()
    {
        int changes = 0;
        var runner = new LibraryScanRunner(null!, new SonorizeSettings(), NullLogger<LibraryScanRunner>.Instance,
            () => Interlocked.Increment(ref changes));
        var first = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var second = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        Task scan1 = runner.RunScanAsync(() => first.Task);
        Task scan2 = runner.RunScanAsync(() => second.Task);
        Assert.True(runner.IsScanning);
        first.SetResult();
        await scan1;
        Assert.True(runner.IsScanning);
        Assert.Equal(1, Volatile.Read(ref changes));
        second.SetResult();
        await scan2;
        Assert.False(runner.IsScanning);
        Assert.Equal(2, Volatile.Read(ref changes));
    }

    [Fact]
    public async Task Failed_scan_is_caught_and_marks_library_dirty()
    {
        int changes = 0;
        var runner = new LibraryScanRunner(null!, new SonorizeSettings(), NullLogger<LibraryScanRunner>.Instance,
            () => Interlocked.Increment(ref changes));

        await runner.RunScanAsync(() => Task.FromException(new InvalidOperationException("test failure")));

        Assert.False(runner.IsScanning);
        Assert.Equal(1, changes);
    }
}
