using System.Text.Json;
using Microsoft.AspNetCore.Components;
using var doc = JsonDocument.Parse(_themeJson);

namespace Sonorize.Photino.Blazor.Theming;

public partial class ThemeImportModal
{
    [Parameter] public bool IsVisible { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback<string> OnImport { get; set; }

    private string _themeJson = "";
    private bool _hasError = false;

    protected override void OnParametersSet()
    {
        if (!IsVisible)
        {
            return;
        }

        _themeJson = "";
        _hasError = false;
    }

    private async Task Confirm()
    {
        if (string.IsNullOrWhiteSpace(_themeJson))
        {
            await Cancel();
            return;
        }

        try
        {
            // Basic validation that it is valid JSON
            _hasError = false;
            await OnImport.InvokeAsync(_themeJson);
        }
        catch
        {
            _hasError = true;
        }
    }

    private async Task Cancel()
    {
        await OnClose.InvokeAsync();
    }
}
