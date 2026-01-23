using Microsoft.Extensions.DependencyInjection;
using Sonorize.Core.Services;
using Sonorize.Core.Services.Audio;
using Sonorize.Core.Services.Library;
using Sonorize.Core.Services.Scrobbling;
using Sonorize.Core.Services.System;
using Sonorize.Core.Services.UI;
using Sonorize.Core.Services.Update;
using Sonorize.Core.Settings;

namespace Sonorize.Photino.Blazor.Setup;

public static class ServiceRegistrar
{
    public static void Configure(IServiceCollection services)
    {
        RegisterCoreInfrastructure(services);
        RegisterSettings(services);
        RegisterLibrary(services);
        RegisterAudio(services);
        RegisterUI(services);
        RegisterIntegrations(services);
    }

    private static void RegisterCoreInfrastructure(IServiceCollection services)
    {
        _ = services.AddSingleton<LogService>();
    }

    private static void RegisterSettings(IServiceCollection services)
    {
        _ = services.AddSingleton<ISettingsManager<SonorizeSettings>>(_ => new SettingsManager<SonorizeSettings>("Settings.json"));
        _ = services.AddSingleton(sp => sp.GetRequiredService<ISettingsManager<SonorizeSettings>>().Load());
    }

    private static void RegisterLibrary(IServiceCollection services)
    {
        // Maximum decomposition pattern
        _ = services.AddSingleton<LibraryDataManager>();
        _ = services.AddSingleton<LibraryScanCoordinator>();
        _ = services.AddSingleton<LibraryEventCoordinator>();
        _ = services.AddSingleton<FolderTreeBuilder>();
        _ = services.AddSingleton<PlaylistSyncOrchestrator>();
        _ = services.AddSingleton<FolderScanner>();
        _ = services.AddSingleton<DemoDataLoader>();

        // Facade service
        _ = services.AddSingleton<LibraryService>();

        // Supporting services
        _ = services.AddSingleton<IMusicLibraryService, MusicLibraryService>();
        _ = services.AddSingleton<PlaylistPersistenceService>();
        _ = services.AddSingleton<PlaylistManager>();
        _ = services.AddSingleton<SearchService>();
        _ = services.AddSingleton<LibraryCacheService>();
        _ = services.AddSingleton<LibraryAggregator>();
        _ = services.AddSingleton<LibraryFileMonitor>();
        _ = services.AddSingleton<LibraryScanner>();
    }

    private static void RegisterAudio(IServiceCollection services)
    {
        _ = services.AddSingleton<IAudioService, AudioService>();
        _ = services.AddSingleton<EqPresetService>();
        _ = services.AddSingleton<QueueController>();
        _ = services.AddSingleton<PlayerServiceFactory>();
        _ = services.AddSingleton<IPlayerService>(sp => sp.GetRequiredService<PlayerServiceFactory>().Create());
        _ = services.AddSingleton<PlayerSettingsPersistenceService>();
    }

    private static void RegisterUI(IServiceCollection services)
    {
        _ = services.AddSingleton<ThemeService>();
        _ = services.AddSingleton<LayoutStateService>();
        _ = services.AddSingleton<FileImportService>();
        _ = services.AddSingleton<ImageAnalysisService>();
        _ = services.AddSingleton<GitHubUpdateService>();
    }

    private static void RegisterIntegrations(IServiceCollection services)
    {
        _ = services.AddSingleton<FileExplorerService>();
        _ = services.AddSingleton<LastfmAuthService>();
        _ = services.AddSingleton<ScrobblingService>();
        _ = services.AddSingleton<ScrobbleEligibilityService>();
        _ = services.AddSingleton<ScrobbleOrchestrator>();
    }
}