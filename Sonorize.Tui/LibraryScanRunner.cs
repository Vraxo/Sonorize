using Microsoft.Extensions.Logging;
using Sonorize.Core.Library;
using Sonorize.Core.Settings;

namespace Sonorize.Tui;

/// <summary>
/// Runs library operations off the UI thread while the status line shows
/// "Scanning". Signals the UI update coordinator
/// when a scan finishes so the table refreshes on the next UI tick.
/// </summary>
internal sealed class LibraryScanRunner
{
    private readonly LibraryService _library;
    private readonly SonorizeSettings _settings;
    private readonly ILogger _logger;
    private readonly Action _markLibraryDirty;

    private int _activeScans;

    public LibraryScanRunner(
        LibraryService library,
        SonorizeSettings settings,
        ILogger logger,
        Action markLibraryDirty)
    {
        _library = library;
        _settings = settings;
        _logger = logger;
        _markLibraryDirty = markLibraryDirty;
    }

    public bool IsScanning => Volatile.Read(ref _activeScans) > 0;

    public async Task LoadLibraryAsync()
    {
        await RunScanAsync(async () =>
        {
            await _library.InitializeAsync().ConfigureAwait(false);

            if (_settings.Library.ScanOnStartup && _settings.Library.MusicFolderPaths.Count > 0)
            {
                await _library.RefreshLibraryAsync().ConfigureAwait(false);
            }
        }).ConfigureAwait(false);
    }

    /// <summary>Runs a library operation off the UI thread while the status line shows "Scanning".</summary>
    public async Task RunScanAsync(Func<Task> scan)
    {
        Interlocked.Increment(ref _activeScans);

        try
        {
            await Task.Run(scan).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TUI] Library scan failed");
        }
        finally
        {
            Interlocked.Decrement(ref _activeScans);
            _markLibraryDirty();
        }
    }
}
