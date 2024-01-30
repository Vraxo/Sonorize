using NAudio.Wave;

namespace Sonorize;

class Player : Component
{
    // Fields

    public string SongName { get; private set; } = "...";
    public bool Loop = false;
    public IWavePlayer WaveOutDevice = new WaveOutEvent();
    private AudioFileReader audioFile;
    private string songPath = string.Empty;
    private bool isSongChangingManually = false;

    // Public

    public void PlaySelected()
    {
        if (program.SongsList.Songs.Count < 1)
        {
            return;
        }

        int cursor = program.SelectionCursor.Y + program.SongsListPrinter.SongsInPreviousPages;
        songPath = program.SongsList.Songs[cursor];
        Play();
    }

    public void Pause()
    {
        if (WaveOutDevice.PlaybackState == PlaybackState.Paused)
        {
            WaveOutDevice.Play();
        }
        else
        {
            WaveOutDevice.Pause();
        }
    }

    // Private

    private void Play()
    {
        try
        {
            int cursor = program.SelectionCursor.Y;

            SongName = Path.GetFileNameWithoutExtension(songPath);
            audioFile = new(songPath);

            if (WaveOutDevice != null)
            {
                isSongChangingManually = true;
                WaveOutDevice.Dispose();
                isSongChangingManually = false;
            }

            WaveOutDevice = new WaveOutEvent();

            WaveOutDevice.Init(audioFile);
            WaveOutDevice.Volume = program.VolumeController.Volume / 100F;
            WaveOutDevice.Play();
            WaveOutDevice.PlaybackStopped += OnSongEnded;
        }
        catch { }
    }

    private void PlayNext()
    {
        int indexOfNextSong = program.SongsList.Songs.IndexOf(songPath) + 1;

        if (indexOfNextSong == program.SongsList.Songs.Count)
        {
            return;
        }

        int index = indexOfNextSong < program.SongsList.Songs.Count ?
                    indexOfNextSong :
                    0;

        songPath = program.SongsList.Songs[index];
        Play();
    }

    private void PlayNextForever()
    {
        int indexOfNextSong = program.SongsList.Songs.IndexOf(songPath) + 1;

        int index = indexOfNextSong < program.SongsList.Songs.Count ?
                    indexOfNextSong :
                    0;

        songPath = program.SongsList.Songs[index];
        Play();
    }

    private void PlayRandom()
    {
        int index = new Random().Next(program.PlaylistCollection.CurrentPlaylist.SongPaths.Count);
        songPath = program.SongsList.Songs[index];
        Play();
    }

    // Events

    private void OnSongEnded(object? sender, StoppedEventArgs e)
    {
        if (isSongChangingManually) 
        { 
            return; 
        }

        switch (program.Replayer.Mode)
        {
            case ReplayMode.Loop:
                Play();
                break;

            case ReplayMode.Cycle:
                PlayNext();
                break;

            case ReplayMode.Infinite:
                PlayNextForever();
                break;

            case ReplayMode.Shuffle:
                PlayRandom();
                break;
        }

        //program.InputHandler.Clear();
        program.ConsoleUI.Update();
    }
}