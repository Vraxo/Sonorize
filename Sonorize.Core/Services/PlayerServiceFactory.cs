using Microsoft.Extensions.DependencyInjection;
using Sonorize.Core.Services.Audio;
using Sonorize.Core.Settings;

namespace Sonorize.Core.Services;

public class PlayerServiceFactory
{
    private readonly IServiceProvider _services;

    public PlayerServiceFactory(IServiceProvider services)
    {
        _services = services;
    }

    public IPlayerService Create()
    {
        SonorizeSettings settings = _services.GetRequiredService<SonorizeSettings>();
        IAudioService audioService = _services.GetRequiredService<IAudioService>();
        QueueController queueController = _services.GetRequiredService<QueueController>();

        // Apply initial EQ settings before creating PlayerService
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
    }
}