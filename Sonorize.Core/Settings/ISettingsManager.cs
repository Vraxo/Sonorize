namespace Sonorize.Core.Settings;

public interface ISettingsManager<T> where T : class, new()
{
    void Save(T settings);
    T Load();
    void Delete(); // NEW: Useful for factory resets
    event Action? SettingsSaved;
}