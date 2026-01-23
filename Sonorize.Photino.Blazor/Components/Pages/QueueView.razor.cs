using Sonorize.Core.Models;

namespace Sonorize.Photino.Blazor.Components.Pages;

public partial class QueueView
{
    private bool _showActionsModal = false;
    private bool _showMetadataEditor = false;
    private Song? _selectedSongForModal;

    protected override void OnInitialized()
    {
        PlayerService.QueueChanged += OnQueueChanged;
        PlayerService.PlaybackStateChanged += OnStateChanged;
    }

    private void OnQueueChanged()
    {
        _ = InvokeAsync(StateHasChanged);
    }

    private void OnStateChanged()
    {
        _ = InvokeAsync(StateHasChanged);
    }

    private void GoBack()
    {
        Nav.NavigateTo("/");
    }

    private async Task PlayFromQueue(int index)
    {
        Song song = PlayerService.PlaybackQueue[index];
        // PlaySong usually resets the queue, but here we just want to JUMP within the existing queue.
        // The IPlayerService doesn't explicitly expose "JumpToIndex".
        // However, PlaySong with the SAME list context finds the index and plays it.
        await PlayerService.PlaySong(song, PlayerService.PlaybackQueue);
    }

    private void HandleReorder((Song Source, Song Target) args)
    {
        int oldIndex = PlayerService.PlaybackQueue.IndexOf(args.Source);
        int newIndex = PlayerService.PlaybackQueue.IndexOf(args.Target);

        if (oldIndex == -1 || newIndex == -1)
        {
            return;
        }

        PlayerService.ReorderQueue(oldIndex, newIndex);
    }

    private void HandleRemove(Song song)
    {
        int index = PlayerService.PlaybackQueue.IndexOf(song);

        if (index == -1)
        {
            return;
        }

        PlayerService.RemoveFromQueue(index);
    }

    private void OpenOptions(Song song)
    {
        _selectedSongForModal = song;
        _showActionsModal = true;
    }
    private void CloseOptions()
    {
        _showActionsModal = false;
    }

    private void OpenMetadataEditor(Song song)
    {
        _selectedSongForModal = song;
        _showMetadataEditor = true;
    }

    private void CloseMetadataEditor()
    {
        _showMetadataEditor = false;
        _selectedSongForModal = null;
    }

    public void Dispose()
    {
        PlayerService.QueueChanged -= OnQueueChanged;
        PlayerService.PlaybackStateChanged -= OnStateChanged;
    }
}