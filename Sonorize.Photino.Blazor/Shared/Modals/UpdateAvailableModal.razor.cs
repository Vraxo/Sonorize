using Sonorize.Core.Models;
using Sonorize.Core.Services.Update;

namespace Sonorize.Photino.Blazor.Shared.Modals;

public partial class UpdateAvailableModal
{
    private ReleaseInfo? _release;
    private bool _isDismissed = false;
    private bool _isDownloading = false;
    private double _downloadProgress = 0;

    private string _confirmText => _isDownloading ? "Downloading..." : "Update & Restart";

    protected override async Task OnInitializedAsync()
    {
        // Listen for events (Real or Mock)
        UpdateService.UpdateDetected += OnUpdateDetected;

        // Perform initial real check silently if enabled
        if (AppSettings.Updates.CheckForUpdates)
        {
            await UpdateService.CheckForUpdateAsync();
        }
    }

    private void OnUpdateDetected(ReleaseInfo release)
    {
        _release = release;
        _isDismissed = false;

        AppSettings.Updates.LastCheckTime = DateTime.Now;
        SettingsManager.Save(AppSettings);

        _ = InvokeAsync(StateHasChanged);
    }

    private async Task HandleConfirm()
    {
        if (_isDownloading || _release is null)
        {
            return;
        }

        _isDownloading = true;
        StateHasChanged();

        try
        {
            string tempPath = Path.Combine(Path.GetTempPath(), $"sonorize_update_{Guid.NewGuid()}.zip");
            var progress = new Progress<double>(p =>
            {
                _downloadProgress = p;
                StateHasChanged();
            });

            await UpdateService.DownloadUpdateAsync(_release.DownloadUrl, tempPath, progress);

            // Launch script
            ScriptUpdateInstaller.InstallAndRestart(tempPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Update failed: {ex.Message}");
            Dismiss();
        }
    }

    private void Dismiss()
    {
        _isDismissed = true;
    }

    private string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    public void Dispose()
    {
        UpdateService.UpdateDetected -= OnUpdateDetected;
    }
}