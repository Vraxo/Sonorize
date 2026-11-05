using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;

namespace Sonorize.Photino.Blazor.Theming;

public partial class SaveThemeModal
{
    [Parameter] public bool IsVisible { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback<string> OnConfirm { get; set; }

    private string _themeName = "";

    protected override void OnParametersSet()
    {
        if (IsVisible)
        {
            _themeName = "";
        }
    }

    private void HandleKeyUp(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            _ = Confirm();
        }
        else if (e.Key == "Escape")
        {
            _ = Cancel();
        }
    }

    private async Task Confirm()
    {
        if (string.IsNullOrWhiteSpace(_themeName))
        {
            return;
        }

        await OnConfirm.InvokeAsync(_themeName);
    }

    private async Task Cancel()
    {
        await OnClose.InvokeAsync();
    }
}
