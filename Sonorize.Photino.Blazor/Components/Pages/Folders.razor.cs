namespace Sonorize.Photino.Blazor.Components.Pages;

public partial class Folders
{
    protected override void OnInitialized()
    {
        LibService.LibraryChanged += OnLibraryChanged;
    }

    private void OnLibraryChanged()
    {
        _ = InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        LibService.LibraryChanged -= OnLibraryChanged;
    }
}