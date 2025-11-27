using Sonorize.Core.Models;
using Sonorize.Core.Settings;

namespace Sonorize.Core.Services.Scrobbling;

public class ScrobbleEligibilityService
{
    private const int MinTrackLengthForScrobbleSeconds = 30;

    public bool ShouldScrobble(Song song, TimeSpan playedDuration, LastfmSettings settings)
    {
        if (song == null || song.Duration.TotalSeconds <= MinTrackLengthForScrobbleSeconds)
        {
            return false;
        }

        _ = playedDuration.TotalSeconds / song.Duration.TotalSeconds * 100.0;
        double requiredPlaybackFromPercentage = song.Duration.TotalSeconds * (settings.ScrobbleThresholdPercentage / 100.0);
        double requiredPlaybackAbsolute = settings.ScrobbleThresholdAbsoluteSeconds;

        // Logic: The track must be played for at least half its duration, or for 4 minutes (whichever is shorter).
        double effectiveRequiredSeconds = Math.Min(requiredPlaybackFromPercentage, requiredPlaybackAbsolute);

        return playedDuration.TotalSeconds >= effectiveRequiredSeconds;
    }
}