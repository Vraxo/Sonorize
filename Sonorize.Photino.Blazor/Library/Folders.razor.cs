namespace Sonorize.Photino.Blazor.Library;

public partial class Folders
{
    protected override void OnInitialized()
    {
        LibService.LibraryChanged += OnLibraryChanged;
    }

    private void OnLibraryChanged()
    {
        InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        LibService.LibraryChanged -= OnLibraryChanged;
    }
}
