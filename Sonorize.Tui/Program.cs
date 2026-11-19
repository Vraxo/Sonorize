using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Sonorize.Core.Core;
using Sonorize.Core.Playback;
using Sonorize.Core.Scrobbling;
using Sonorize.Core.Settings;
using Terminal.Gui.App;
using Terminal.Gui.Drivers;

namespace Sonorize.Tui;

public static class Program
{
    private const string MutexName = "Sonorize_SingleInstance_Mutex";
    private const string LogLevelVariable = "SONORIZE_LOG_LEVEL";

    private static int Main()
    {
        using Mutex mutex = new(true, MutexName, out bool createdNew);
        if (!createdNew)
        {
            Console.Error.WriteLine("Sonorize is already running.");
            return 1;
        }

        Directory.CreateDirectory(AppDataHelper.GetBaseDirectory());
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(GetMinimumLogLevel())
            .Enrich.FromLogContext()
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
            ServiceCollection services = new();
            TuiServiceRegistrar.Configure(services);
            services.AddLogging(logging =>
                logging.ClearProviders().AddSerilog(Log.Logger));

            using ServiceProvider provider = services.BuildServiceProvider();
            ILogger<MainWindow> logger = provider.GetRequiredService<ILogger<MainWindow>>();
            logger.LogInformation("TUI starting.");

            provider.GetRequiredService<PlayerSettingsPersistenceService>();
            provider.GetRequiredService<ScrobbleOrchestrator>();

            Driver.Force16Colors = true;

            using IApplication app = Application.Create().Init();
            using MainWindow window = ActivatorUtilities.CreateInstance<MainWindow>(provider);
            app.Run(window);

            provider.GetRequiredService<ISettingsManager<SonorizeSettings>>()
                .Save(provider.GetRequiredService<SonorizeSettings>());

            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Fatal TUI crash.");
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static LogEventLevel GetMinimumLogLevel()
    {
        string? configuredLevel = Environment.GetEnvironmentVariable(LogLevelVariable);
        return Enum.TryParse(configuredLevel, ignoreCase: true, out LogEventLevel level)
            ? level
            : LogEventLevel.Information;
    }
}
