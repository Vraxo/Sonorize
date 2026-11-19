using Microsoft.Extensions.DependencyInjection;
using Photino.Blazor;
using Sonorize.Core.Core;
using Sonorize.Core.Library;
using Sonorize.Core.Playback;
using Sonorize.Core.Scrobbling;
using Sonorize.Core.Settings;
using Sonorize.Photino.Blazor.Shell;

namespace Sonorize.Photino.Blazor;

public class Program
{
    // Removed "Global\\" prefix to ensure compatibility with Linux/macOS
    private const string MutexName = "Sonorize_SingleInstance_Mutex";
    private const string PhotinoLogVerbosityVariable = "SONORIZE_PHOTINO_LOG_VERBOSITY";

    [STAThread]
    private static void Main(string[] args)
    {
        using Mutex mutex = new(true, MutexName, out bool createdNew);
        if (!createdNew)
        {
            return;
        }

        LogService logger = new();

        try
        {
            logger.Info("Application startup started.");
            logger.Info($"Application base directory: {AppContext.BaseDirectory}");
            logger.Info($"Application log file: {Path.Combine(AppDataHelper.GetBaseDirectory(), "app.log")}");

            PhotinoBlazorAppBuilder appBuilder = PhotinoBlazorAppBuilder.CreateDefault(args);
            PhotinoBlazorApp app = RegisterServices(appBuilder);

            logger.Info("Services registered.");
            InitializeAppLogic(app.Services);
            SetupWindowAndPersistence(app);
            RegisterCustomSchemes(app);

            logger.Info("Starting Photino window.");
            app.Run();
            logger.Info("Photino window closed.");
        }
        catch (Exception ex)
        {
            logger.Error("Fatal application crash.", ex);
            throw;
        }
    }

    private static PhotinoBlazorApp RegisterServices(PhotinoBlazorAppBuilder appBuilder)
    {
        ServiceRegistrar.Configure(appBuilder.Services);
        appBuilder.RootComponents.Add<App>("app");
        return appBuilder.Build();
    }

    private static void InitializeAppLogic(IServiceProvider services)
    {
        LogService logger = services.GetRequiredService<LogService>();
        logger.Info("Initializing application services.");

        _ = services.GetRequiredService<PlayerSettingsPersistenceService>();
        logger.Info("Player settings persistence initialized.");

        _ = services.GetRequiredService<ScrobbleOrchestrator>();
        logger.Info("Scrobbling services initialized.");

        LibraryService library = services.GetRequiredService<LibraryService>();
        SonorizeSettings settings = services.GetRequiredService<SonorizeSettings>();

        _ = InitializeLibraryAsync(library, settings, logger);
    }

    private static async Task InitializeLibraryAsync(
        LibraryService library,
        SonorizeSettings settings,
        LogService logger)
    {
        try
        {
            logger.Info("Loading library cache and starting file monitoring.");
            await library.InitializeAsync();
            logger.Info($"Library cache loaded: {library.AllSongs.Count} songs.");

            if (!settings.Library.ScanOnStartup)
            {
                logger.Info("Startup library scan is disabled in settings.");
                return;
            }

            if (settings.Library.MusicFolderPaths.Count == 0)
            {
                logger.Warn("Startup library scan skipped because no music folders are configured.");
                return;
            }

            logger.Info($"Starting startup library scan for {settings.Library.MusicFolderPaths.Count} folder(s).");
            await library.RefreshLibraryAsync();
            logger.Info($"Startup library scan completed. Library now contains {library.AllSongs.Count} songs.");
        }
        catch (Exception ex)
        {
            logger.Error("Library initialization or startup scan failed.", ex);
        }
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
                SchemeHandlers.ProcessRequest(url, library, out contentType));
    }

    internal static int GetPhotinoLogVerbosity()
    {
        string? configuredValue = Environment.GetEnvironmentVariable(PhotinoLogVerbosityVariable);

        return int.TryParse(configuredValue, out int verbosity)
            ? verbosity
            : 0;
    }
}
