using Microsoft.Extensions.DependencyInjection;
using Sonorize.Core.Core;
using Sonorize.Core.Playback;
using Sonorize.Core.Scrobbling;
using Sonorize.Core.Settings;
using Terminal.Gui.App;
using Terminal.Gui.Drivers;

namespace Sonorize.Tui;

public static class Program
{
    // Same name as the desktop app: both frontends share Settings.json and the
    // library cache, so only one of them may run at a time.
    private const string MutexName = "Sonorize_SingleInstance_Mutex";

    private static int Main()
    {
        using Mutex mutex = new(true, MutexName, out bool createdNew);

        if (!createdNew)
        {
            Console.Error.WriteLine("Sonorize is already running.");
            return 1;
        }

        ServiceCollection services = new();
        TuiServiceRegistrar.Configure(services);

        using ServiceProvider provider = services.BuildServiceProvider();
        LogService logger = provider.GetRequiredService<LogService>();

        try
        {
            logger.Info("TUI starting...");

            // Instantiate the background listeners so they subscribe to player events.
            provider.GetRequiredService<PlayerSettingsPersistenceService>();
            provider.GetRequiredService<ScrobbleOrchestrator>();

            // Emit the 16 ANSI colour codes instead of 24-bit RGB, so colours come
            // from the user's terminal theme like in any other console program.
            Driver.Force16Colors = true;

            using IApplication app = Application.Create().Init();
            using MainWindow window = ActivatorUtilities.CreateInstance<MainWindow>(provider);

            app.Run(window);

            // The player persists its settings on a debounce timer; flush on the way out
            // so a volume change made right before quitting is not lost.
            provider.GetRequiredService<ISettingsManager<SonorizeSettings>>()
                .Save(provider.GetRequiredService<SonorizeSettings>());

            return 0;
        }
        catch (Exception ex)
        {
            logger.Error("FATAL TUI CRASH", ex);
            throw;
        }
    }
}
