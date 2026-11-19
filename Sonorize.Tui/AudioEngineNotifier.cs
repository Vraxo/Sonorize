using Terminal.Gui.App;
using Terminal.Gui.Views;

namespace Sonorize.Tui;

/// <summary>Shows the "audio engine unavailable" dialog. Shared by the
/// browsing and input controllers so neither depends on the other.</summary>
internal sealed class AudioEngineNotifier
{
    private readonly Func<IApplication?> _appProvider;

    public AudioEngineNotifier(Func<IApplication?> appProvider)
    {
        _appProvider = appProvider;
    }

    public void ShowUnavailable()
    {
        MessageBox.ErrorQuery(
            _appProvider()!,
            "Audio engine unavailable",
            "The BASS native library could not be loaded, so playback is disabled.\n"
            + "Place the BASS binaries for your platform next to the executable\n"
            + "(see \"Setup Audio Dependencies\" in the README) and restart.",
            "OK");
    }
}
