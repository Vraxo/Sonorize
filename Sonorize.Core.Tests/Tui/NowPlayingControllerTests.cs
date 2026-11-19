using NSubstitute;
using Sonorize.Core.Core;
using Sonorize.Core.Playback;
using Sonorize.Core.Settings;
using Sonorize.Tui;

namespace Sonorize.Core.Tests.Tui;

public class NowPlayingControllerTests
{
    [Fact]
    public void Updates_playback_labels_and_error_visibility()
    {
        var player = Substitute.For<IPlayerService>();
        player.IsAudioEngineAvailable.Returns(true);
        player.CurrentSong.Returns(new Song { FilePath = "song.mp3", Title = "Track", Artist = "Artist", Album = "Album" });
        player.IsPlaying.Returns(true);
        player.CurrentTime.Returns(TimeSpan.FromSeconds(30));
        player.TotalTime.Returns(TimeSpan.FromSeconds(120));
        player.Volume.Returns(0.5f);
        player.IsShuffle.Returns(true);
        player.RepeatMode.Returns(RepeatMode.One);
        var scans = new LibraryScanRunner(null!, new SonorizeSettings(), new LogService(), () => { });
        var layout = new MainWindowLayout();
        try
        {
            var bar = new NowPlayingController(player, scans, layout);
            bar.UpdateNowPlaying();
            Assert.Equal("▶ Track — Artist · Album", layout.NowPlaying.Text);
            Assert.Equal("0:30 / 2:00", layout.Time.Text);
            Assert.Equal(0.25f, layout.Progress.Fraction);
            Assert.Equal("Volume 50%", layout.Status.Text);
            Assert.Equal("Shuffle on", layout.Shuffle.Text);
            Assert.Equal("Repeat one", layout.Repeat.Text);

            player.IsAudioEngineAvailable.Returns(false);
            bar.UpdateNowPlaying();
            Assert.Contains("Audio engine unavailable", layout.Status.Text);
            Assert.False(layout.Shuffle.Visible);
            Assert.False(layout.Repeat.Visible);

            player.IsAudioEngineAvailable.Returns(true);
            bar.UpdateNowPlaying();
            Assert.True(layout.Shuffle.Visible);
            Assert.True(layout.Repeat.Visible);
        }
        finally
        {
            foreach (var view in layout.Views) view.Dispose();
        }
    }
}
