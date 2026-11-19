using Microsoft.Extensions.DependencyInjection;
using Sonorize.Core.Library;
using Sonorize.Core.Playback;
using Sonorize.Core.Playlists;
using Sonorize.Core.Scrobbling;
using Sonorize.Core.Settings;

namespace Sonorize.Tui;

/// <summary>
/// Registers the Sonorize.Core services the terminal frontend uses.
/// Mirrors the desktop app's registrar, minus the services that only make sense
/// with a graphical UI (theming, layout state, image analysis, updater).
/// </summary>
public static class TuiServiceRegistrar
{
    public static void Configure(IServiceCollection services)
    {
        RegisterSettings(services);
        RegisterLibrary(services);
        RegisterAudio(services);
        RegisterIntegrations(services);
    }

    private static void RegisterSettings(IServiceCollection services)
    {
        services.AddSingleton<ISettingsManager<SonorizeSettings>>(_ => new SettingsManager<SonorizeSettings>("Settings.json"));
        services.AddSingleton(sp => sp.GetRequiredService<ISettingsManager<SonorizeSettings>>().Load());
    }

    private static void RegisterLibrary(IServiceCollection services)
    {
        services.AddSingleton<LibraryDataManager>();
        services.AddSingleton<LibraryScanCoordinator>();
        services.AddSingleton<LibraryEventCoordinator>();
        services.AddSingleton<FolderTreeBuilder>();
        services.AddSingleton<PlaylistSyncOrchestrator>();
        services.AddSingleton<FolderScanner>();
        services.AddSingleton<DemoDataLoader>();

        services.AddSingleton<LibraryService>();

        services.AddSingleton<IMusicLibraryService, MusicLibraryService>();
        services.AddSingleton<PlaylistPersistenceService>();
        services.AddSingleton<PlaylistManager>();
        services.AddSingleton<SearchService>();
        services.AddSingleton<LibraryCacheService>();
        services.AddSingleton<LibraryAggregator>();
        services.AddSingleton<LibraryFileMonitor>();
        services.AddSingleton<LibraryScanner>();
    }

    private static void RegisterAudio(IServiceCollection services)
    {
        services.AddSingleton<IAudioService, AudioService>();
        services.AddSingleton<QueueController>();
        services.AddSingleton<PlayerServiceFactory>();
        services.AddSingleton(sp => sp.GetRequiredService<PlayerServiceFactory>().Create());
        services.AddSingleton<PlayerSettingsPersistenceService>();
    }

    private static void RegisterIntegrations(IServiceCollection services)
    {
        services.AddSingleton<LastfmAuthService>();
        services.AddSingleton<ScrobblingService>();
        services.AddSingleton<ScrobbleEligibilityService>();
        services.AddSingleton<ScrobbleOrchestrator>();
    }
}
