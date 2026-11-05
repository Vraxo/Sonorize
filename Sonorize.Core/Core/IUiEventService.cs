namespace Sonorize.Core.Core;

public interface IUiEventService
{
    void SendEvent(string eventName, object? payload);
}
