using Microsoft.AspNetCore.Components;

namespace Sonorize.Photino.Blazor.Shared;

public partial class Modal
{
    [Parameter] public bool IsVisible { get; set; }
    [Parameter] public string Title { get; set; } = "Modal";
    [Parameter] public string ConfirmText { get; set; } = "OK";
    [Parameter] public bool ShowCancelButton { get; set; } = true;
    [Parameter] public RenderFragment? ChildContent { get; set; }

    [Parameter] public EventCallback OnCancel { get; set; }
    [Parameter] public EventCallback OnConfirm { get; set; }
}