using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Photino.Blazor;
using Serilog;
using Sonorize.Core.Core;
using Sonorize.Core.Library;
using Sonorize.Core.Playback;
using Sonorize.Core.Scrobbling;
using Sonorize.Core.Settings;
using Sonorize.Photino.Blazor.Shell;

namespace Sonorize.Photino.Blazor;

public class Program
{
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

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                Path.Combine(AppDataHelper.GetBaseDirectory(), "app-.log"),
                rollingInterval: RollingInterval.Day,
                fileSizeLimitBytes: 5 * 1024 * 1024,
                rollOnFileSizeLimit: true,
                retainedFileCountLimit: 14,
                shared: true,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        try
        {
            Log.Information("Application startup started. Base directory: {BaseDirectory}", AppContext.BaseDirectory);

            PhotinoBlazorAppBuilder appBuilder = PhotinoBlazorAppBuilder.CreateDefault(args);
            appBuilder.Services.AddLogging(logging =>
                logging.ClearProviders().AddSerilog(Log.Logger));

            PhotinoBlazorApp app = RegisterServices(appBuilder);

            Log.Information("Services registered.");
            InitializeAppLogic(app.Services);
            SetupWindowAndPersistence(app);
            RegisterCustomSchemes(app);

            Log.Information("Starting Photino window.");
            app.Run();
            Log.Information("Photino window closed.");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Fatal application crash.");
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
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
        ILogger<Program> logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Initializing application services.");

        _ = services.GetRequiredService<PlayerSettingsPersistenceService>();
        logger.LogInformation("Player settings persistence initialized.");

        _ = services.GetRequiredService<ScrobbleOrchestrator>();
        logger.LogInformation("Scrobbling services initialized.");

        LibraryService library = services.GetRequiredService<LibraryService>();
        SonorizeSettings settings = services.GetRequiredService<SonorizeSettings>();

        _ = InitializeLibraryAsync(library, settings, logger);
    }

    private static async Task InitializeLibraryAsync(
        LibraryService library,
        SonorizeSettings settings,
        ILogger<Program> logger)
    {
        try
        {
            logger.LogInformation("Loading library cache and starting file monitoring.");
            await library.InitializeAsync();
            logger.LogInformation("Library cache loaded: {SongCount} songs.", library.AllSongs.Count);

            if (!settings.Library.ScanOnStartup)
            {
                logger.LogInformation("Startup library scan is disabled in settings.");
                return;
            }

            if (settings.Library.MusicFolderPaths.Count == 0)
            {
                logger.LogWarning("Startup library scan skipped because no music folders are configured.");
                return;
            }

            logger.LogInformation(
                "Starting startup library scan for {FolderCount} folder(s).",
                settings.Library.MusicFolderPaths.Count);
            await library.RefreshLibraryAsync();
            logger.LogInformation("Startup library scan completed. Library now contains {SongCount} songs.", library.AllSongs.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Library initialization or startup scan failed.");
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
        return int.TryParse(configuredValue, out int verbosity) ? verbosity : 0;
    }
}
