using Microsoft.Extensions.DependencyInjection;
using Photino.Blazor;
using Sonorize.Core.Services.Audio;
using Sonorize.Core.Services.Library;
using Sonorize.Core.Services.Scrobbling;
using Sonorize.Core.Services.System;
using Sonorize.Core.Settings;
using Sonorize.Photino.Blazor.Components;
using Sonorize.Photino.Blazor.Setup;

namespace Sonorize.Photino.Blazor;

public class Program
{
    // Removed "Global\" prefix to ensure compatibility with Linux/macOS
    private const string MutexName = "Sonorize_SingleInstance_Mutex";

    [STAThread]
    private static void Main(string[] args)
    {
        using Mutex mutex = new(true, MutexName, out bool createdNew);

        if (!createdNew)
        {
            // Another instance is already running
            // In a production app, we might want to bring the other window to front here,
            // but for now, we simply exit to prevent data corruption.
            return;
        }

        try
        {
            PhotinoBlazorAppBuilder appBuilder = PhotinoBlazorAppBuilder.CreateDefault(args);

            PhotinoBlazorApp app = RegisterServices(appBuilder);
            InitializeAppLogic(app.Services);
            SetupWindowAndPersistence(app);
            RegisterCustomSchemes(app);

            app.Run();
        }
        catch (Exception ex)
        {
            // Fatal Crash Handler
            // Since DI might have failed, we instantiate a temporary logger manually
            LogService logger = new();
            logger.Error("FATAL APP CRASH", ex);

            // Re-throw to ensure the OS knows the process failed
            throw;
        }
    }

    private static PhotinoBlazorApp RegisterServices(PhotinoBlazorAppBuilder appBuilder)
    {
        ServiceRegistrar.Configure(appBuilder.Services);
        appBuilder.RootComponents.Add<App>("app");
        PhotinoBlazorApp app = appBuilder.Build();
        return app;
    }

    private static void InitializeAppLogic(IServiceProvider services)
    {
        LogService logger = services.GetRequiredService<LogService>();
        logger.Info("App starting...");

        services.GetRequiredService<PlayerSettingsPersistenceService>();

        // Initialize Scrobble Orchestrator so it starts listening to Player events
        services.GetRequiredService<ScrobbleOrchestrator>();

        LibraryService library = services.GetRequiredService<LibraryService>();
        services.GetRequiredService<SonorizeSettings>();

        // InitializeAsync handles both Cache loading and Background scanning internally
        library.InitializeAsync();
    }

    private static void SetupWindowAndPersistence(PhotinoBlazorApp app)
    {
        WindowStateManager windowManager = new(app);
        windowManager.Initialize();
    }

    private static void RegisterCustomSchemes(PhotinoBlazorApp app)
    {
        LibraryService library = app.Services.GetRequiredService<LibraryService>();

        app.MainWindow.RegisterCustomSchemeHandler("sonorize",
           (object sender, string scheme, string url, out string contentType) =>
           {
               return SchemeHandlers.ProcessRequest(url, library, out contentType);
           });
    }
}