using System.Diagnostics;
using Sonorize.Core.Playback;

namespace Sonorize.Photino.Blazor.Playback;

public partial class AudioEngineStatusModal
{
    private bool _fxWarningDismissed = false;

    private void DismissFxWarning()
    {
        _fxWarningDismissed = true;

        StateHasChanged();
    }

    protected override void OnInitialized()
    {
        PlayerService.PlaybackStateChanged += StateHasChanged;
    }

    private void OpenBrowser(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to open URL: {ex.Message}");
        }
    }
}
