using Sonorize.Core.Models;

namespace Sonorize.Core.Services.Audio;

public class QueueController
{
    public List<Song> Queue { get; private set; } = [];
    public int CurrentIndex { get; private set; } = -1;
    public bool IsShuffle { get; private set; }
    public RepeatMode RepeatMode { get; private set; }

    // Shuffle state
    private List<int> _shuffleDeck = [];
    private int _shufflePointer = -1;
    private readonly Random _rng = new();

    public Song? CurrentSong => CurrentIndex >= 0 && CurrentIndex < Queue.Count
        ? Queue[CurrentIndex]
        : null;

    public event Action? QueueChanged;
    public event Action<bool, RepeatMode>? ModesChanged;

    public void SetContext(List<Song> songs, bool keepCurrent = false)
    {
        Queue = songs;

        if (IsShuffle)
        {
            RebuildShuffleDeck();
        }

        if (!keepCurrent)
        {
            CurrentIndex = -1;
            _shufflePointer = -1;
        }
        else
        {
            // If keeping current, ensure pointer is valid
            SyncShufflePointer();
        }

        QueueChanged?.Invoke();
    }

    public void SetIndex(int index)
    {
        CurrentIndex = Math.Clamp(index, -1, Queue.Count - 1);
        SyncShufflePointer();
        QueueChanged?.Invoke();
    }

    public void SetIndexBySong(Song song)
    {
        int index = Queue.FindIndex(s => s.FilePath == song.FilePath);
        SetIndex(index);
    }

    public bool TryAdvance(bool autoAdvance)
    {
        if (Queue.Count == 0)
        {
            return false;
        }

        if (RepeatMode == RepeatMode.One && autoAdvance)
        {
            // Repeat One logic handled by caller usually, but if called here, we stay same.
            return true;
        }

        return IsShuffle ? AdvanceShuffle(forward: true, autoAdvance) : AdvanceLinear(forward: true, autoAdvance);
    }

    public bool TryRegress()
    {
        return Queue.Count != 0 && (IsShuffle ? AdvanceShuffle(forward: false, autoAdvance: false) : AdvanceLinear(forward: false, autoAdvance: false));
    }

    // --- Mode Management ---

    public void ToggleShuffle()
    {
        IsShuffle = !IsShuffle;
        if (IsShuffle)
        {
            RebuildShuffleDeck();
            SyncShufflePointer();
        }
        ModesChanged?.Invoke(IsShuffle, RepeatMode);
    }

    public void ToggleRepeat()
    {
        RepeatMode = RepeatMode switch
        {
            RepeatMode.None => RepeatMode.All,
            RepeatMode.All => RepeatMode.One,
            _ => RepeatMode.None
        };
        ModesChanged?.Invoke(IsShuffle, RepeatMode);
    }

    public void SetModes(bool isShuffle, RepeatMode repeatMode)
    {
        IsShuffle = isShuffle;
        RepeatMode = repeatMode;
        if (IsShuffle)
        {
            RebuildShuffleDeck();
            SyncShufflePointer();
        }
    }

    // --- Mutation ---

    public void Remove(int index)
    {
        if (index < 0 || index >= Queue.Count)
        {
            return;
        }

        bool removingCurrent = index == CurrentIndex;
        Queue.RemoveAt(index);

        if (IsShuffle)
        {
            RebuildShuffleDeck();
        }

        if (Queue.Count == 0)
        {
            CurrentIndex = -1;
        }
        else if (removingCurrent)
        {
            // If we removed the current song, logic dictates we play the next available
            // Note: If at end, wrap or stop? Simple: Clamp to count.
            if (CurrentIndex >= Queue.Count)
            {
                CurrentIndex = 0;
            }
        }
        else if (index < CurrentIndex)
        {
            CurrentIndex--;
        }

        SyncShufflePointer();
        QueueChanged?.Invoke();
    }

    public void Reorder(int oldIndex, int newIndex)
    {
        if (oldIndex == newIndex)
        {
            return;
        }

        Song song = Queue[oldIndex];
        Queue.RemoveAt(oldIndex);
        Queue.Insert(newIndex, song);

        if (CurrentIndex == oldIndex)
        {
            CurrentIndex = newIndex;
        }
        else if (CurrentIndex > oldIndex && CurrentIndex <= newIndex)
        {
            CurrentIndex--;
        }
        else if (CurrentIndex < oldIndex && CurrentIndex >= newIndex)
        {
            CurrentIndex++;
        }

        if (IsShuffle)
        {
            RebuildShuffleDeck();
            SyncShufflePointer();
        }

        QueueChanged?.Invoke();
    }

    // --- Internals ---

    private bool AdvanceLinear(bool forward, bool autoAdvance)
    {
        int next = forward ? CurrentIndex + 1 : CurrentIndex - 1;

        if (next >= Queue.Count)
        {
            if (RepeatMode == RepeatMode.All || (!autoAdvance && forward))
            {
                CurrentIndex = 0;
                return true;
            }
            return false;
        }

        if (next < 0)
        {
            CurrentIndex = Queue.Count - 1;
            return true;
        }

        CurrentIndex = next;
        return true;
    }

    private bool AdvanceShuffle(bool forward, bool autoAdvance)
    {
        if (_shuffleDeck.Count == 0)
        {
            return false;
        }

        int nextPtr = forward ? _shufflePointer + 1 : _shufflePointer - 1;

        if (nextPtr >= _shuffleDeck.Count)
        {
            if (RepeatMode == RepeatMode.All || (!autoAdvance && forward))
            {
                nextPtr = 0;
            }
            else
            {
                return false;
            }
        }
        else if (nextPtr < 0)
        {
            nextPtr = _shuffleDeck.Count - 1;
        }

        _shufflePointer = nextPtr;
        CurrentIndex = _shuffleDeck[_shufflePointer];
        return true;
    }

    private void RebuildShuffleDeck()
    {
        _shuffleDeck = Enumerable.Range(0, Queue.Count).ToList();
        int n = _shuffleDeck.Count;
        while (n > 1)
        {
            n--;
            int k = _rng.Next(n + 1);
            (_shuffleDeck[k], _shuffleDeck[n]) = (_shuffleDeck[n], _shuffleDeck[k]);
        }
    }

    private void SyncShufflePointer()
    {
        if (!IsShuffle)
        {
            return;
        }

        // Find where the current index lives in the deck
        int ptr = _shuffleDeck.IndexOf(CurrentIndex);
        if (ptr != -1)
        {
            _shufflePointer = ptr;
        }
        else if (_shuffleDeck.Count > 0)
        {
            // Reset if lost
            _shufflePointer = 0;
            CurrentIndex = _shuffleDeck[0];
        }
    }
}