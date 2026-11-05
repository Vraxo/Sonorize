using Microsoft.AspNetCore.Components;
using Sonorize.Core.Core;
using Sonorize.Core.Library;
using Sonorize.Core.Playback;

namespace Sonorize.Photino.Blazor.Library;

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
