namespace Sonorize.Core.Services.UI;

public interface IUiEventService
{
    void SendEvent(string eventName, object? payload);
}