using Microsoft.AspNetCore.Components;
using Sonorize.Core.Models;

namespace Sonorize.Photino.Blazor.Components.Pages.Components;

public partial class FolderNodeView
{
    [Parameter] public required FolderNode Node { get; set; }

    private bool _isExpanded = false;

    private void ToggleExpand()
    {
        _isExpanded = !_isExpanded;
    }

    private async Task PlaySong(Song song)
    {
        // The context is all songs in the current folder node
        await PlayerService.PlaySong(song, Node.Songs);
    }
}