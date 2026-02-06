using FwLiteShared.Services;
using Microsoft.JSInterop;

namespace FwLiteMaui.Services;

/// <summary>
/// JSInvokable wrapper around IPreferences for exposing preferences to JavaScript.
/// Only available when running in MAUI (where IPreferences is registered).
/// </summary>
public class PreferencesServiceJsInvokable(IPreferences preferences) : IPreferencesService
{
    [JSInvokable]
    public string? Get(string key)
    {
        return preferences.Get<string?>(key, null);
    }

    [JSInvokable]
    public void Set(string key, string value)
    {
        preferences.Set(key, value);
    }

    [JSInvokable]
    public void Remove(string key)
    {
        preferences.Remove(key);
    }
}
