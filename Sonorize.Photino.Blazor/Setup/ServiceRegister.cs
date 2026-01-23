using Microsoft.Extensions.DependencyInjection;
using Sonorize.Core.Services.Audio;
using Sonorize.Core.Services.Library;
using Sonorize.Core.Services.Scrobbling;
using Sonorize.Core.Services.System;
using Sonorize.Core.Services.UI;
using Sonorize.Core.Services.Update; // NEW
using Sonorize.Core.Settings;

namespace Sonorize.Photino.Blazor.Setup;

public static class ServiceRegistrar
{
    public static void Configure(IServiceCollection services)
    {
        // Infrastructure
        _ = services.AddSingleton<LogService>();

        // Settings
        _ = services.AddSingleton<ISettingsManager<SonorizeSettings>>(sp => new SettingsManager<SonorizeSettings>("Settings.json"));
        _ = services.AddSingleton(sp => sp.GetRequiredService<ISettingsManager<SonorizeSettings>>().Load());

        // Services
        _ = services.AddSingleton<IMusicLibraryService, MusicLibraryService>();
        _ = services.AddSingleton<PlaylistPersistenceService>();
        _ = services.AddSingleton<PlaylistManager>();
        _ = services.AddSingleton<SearchService>();
        _ = services.AddSingleton<LibraryCacheService>();
        _ = services.AddSingleton<LibraryAggregator>();
        _ = services.AddSingleton<LibraryFileMonitor>();
        _ = services.AddSingleton<LibraryScanner>();
        _ = services.AddSingleton<LibraryService>();
        _ = services.AddSingleton<IAudioService, AudioService>();
        _ = services.AddSingleton<ThemeService>();
        _ = services.AddSingleton<EqPresetService>();

        // Core Logic Services
        _ = services.AddSingleton<QueueController>();
        _ = services.AddSingleton<FileImportService>();
        _ = services.AddSingleton<ImageAnalysisService>();

        // Update Service
        _ = services.AddSingleton<GitHubUpdateService>(); // NEW

        // Platform Integrations
        _ = services.AddSingleton<FileExplorerService>();

        // Last.fm Services
        _ = services.AddSingleton<LastfmAuthService>();
        _ = services.AddSingleton<ScrobblingService>();
        _ = services.AddSingleton<ScrobbleEligibilityService>();
        _ = services.AddSingleton<ScrobbleOrchestrator>();

        // Layout State
        _ = services.AddSingleton<LayoutStateService>();

        // Player Factory
        _ = services.AddSingleton<IPlayerService>(sp =>
        {
            SonorizeSettings settings = sp.GetRequiredService<SonorizeSettings>();
            IAudioService audioService = sp.GetRequiredService<IAudioService>();
            QueueController queueController = sp.GetRequiredService<QueueController>();

            // Apply initial EQ settings
            audioService.SetEq(settings.Playback.EqEnabled, settings.Playback.EqGains);

            return new PlayerService(
                audioService,
                queueController,
                settings.Playback.IsShuffle,
                settings.Playback.RepeatMode,
                settings.Playback.Volume,
                settings.Playback.OutputDeviceName,
                settings.Playback.Tempo,
                settings.Playback.Pitch);
        });

        // Persistence
        _ = services.AddSingleton<PlayerSettingsPersistenceService>();
    }
}