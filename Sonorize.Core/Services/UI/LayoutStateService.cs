namespace Sonorize.Core.Services.UI;

public class LayoutStateService
{
    public event Action? EqRequested;
    public event Action? SpeedPitchRequested;
    public event Action? FocusModeRequested;
    public event Action? QueueViewRequested;

    // Track if we are dragging an internal UI element (like a widget)
    // to prevent the global "File Drop" overlay from triggering.
    public bool IsDraggingInternal { get; private set; }

    public void OpenEq()
    {
        EqRequested?.Invoke();
    }

    public void OpenSpeedPitch()
    {
        SpeedPitchRequested?.Invoke();
    }

    public void ToggleFocus()
    {
        FocusModeRequested?.Invoke();
    }

    public void ToggleQueue()
    {
        QueueViewRequested?.Invoke();
    }

    public void SetInternalDrag(bool isDragging)
    {
        IsDraggingInternal = isDragging;
    }
}