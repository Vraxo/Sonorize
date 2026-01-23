using Microsoft.AspNetCore.Components;
using Sonorize.Core.Models;

namespace Sonorize.Photino.Blazor.Shared.Modals;

public partial class TrackActionsModal
{
    [Parameter] public bool IsVisible { get; set; }
    [Parameter] public Song? Song { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback<Song> OnEditMetadata { get; set; } // NEW

    private IEnumerable<Playlist> ManualPlaylists =>
        LibService.AllPlaylists.Where(p => p.Type == PlaylistType.Manual);

    private void AddTo(Playlist playlist)
    {
        if (Song is not null)
        {
            LibService.AddSongToPlaylist(playlist, Song);
        }

        _ = Cancel();
    }

    private void OpenInExplorer()
    {
        if (Song is not null)
        {
            FileExplorer.ShowInFolder(Song.FilePath);
        }

        _ = Cancel();
    }

    private async Task TriggerEdit()
    {
        if (Song is not null)
        {
            await OnEditMetadata.InvokeAsync(Song);
        }

        await Cancel();
    }

    private async Task Cancel()
    {
        await OnClose.InvokeAsync();
    }
}