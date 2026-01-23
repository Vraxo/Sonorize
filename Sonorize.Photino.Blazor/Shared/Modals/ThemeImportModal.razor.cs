using Microsoft.AspNetCore.Components;
using System.Text.Json;

namespace Sonorize.Photino.Blazor.Shared.Modals;

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
            using var doc = JsonDocument.Parse(_themeJson);
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