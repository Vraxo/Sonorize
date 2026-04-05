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
        services.AddSingleton<LogService>();
    }

    private static void RegisterSettings(IServiceCollection services)
    {
        services.AddSingleton<ISettingsManager<SonorizeSettings>>(_ => new SettingsManager<SonorizeSettings>("Settings.json"));
        services.AddSingleton(sp => sp.GetRequiredService<ISettingsManager<SonorizeSettings>>().Load());
    }

    private static void RegisterLibrary(IServiceCollection services)
    {
        // Maximum decomposition pattern
        services.AddSingleton<LibraryDataManager>();
        services.AddSingleton<LibraryScanCoordinator>();
        services.AddSingleton<LibraryEventCoordinator>();
        services.AddSingleton<FolderTreeBuilder>();
        services.AddSingleton<PlaylistSyncOrchestrator>();
        services.AddSingleton<FolderScanner>();
        services.AddSingleton<DemoDataLoader>();

        // Facade service
        services.AddSingleton<LibraryService>();

        // Supporting services
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
        services.AddSingleton<EqPresetService>();
        services.AddSingleton<QueueController>();
        services.AddSingleton<PlayerServiceFactory>();
        services.AddSingleton(sp => sp.GetRequiredService<PlayerServiceFactory>().Create());
        services.AddSingleton<PlayerSettingsPersistenceService>();
    }

    private static void RegisterUI(IServiceCollection services)
    {
        services.AddSingleton<ThemeService>();
        services.AddSingleton<LayoutStateService>();
        services.AddSingleton<FileImportService>();
        services.AddSingleton<ImageAnalysisService>();
        services.AddSingleton<GitHubUpdateService>();
    }

    private static void RegisterIntegrations(IServiceCollection services)
    {
        services.AddSingleton<FileExplorerService>();
        services.AddSingleton<LastfmAuthService>();
        services.AddSingleton<ScrobblingService>();
        services.AddSingleton<ScrobbleEligibilityService>();
        services.AddSingleton<ScrobbleOrchestrator>();
    }
}