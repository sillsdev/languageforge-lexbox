using FwLiteShared.Services;
using Microsoft.JSInterop;

namespace FwLiteMaui.Services;

/// <summary>
/// MAUI Essentials-backed preferences service.
/// Delegates to MAUI's IPreferences for platform-native preference storage.
/// Only available when running in MAUI.
/// </summary>
public class MauiPreferencesService(IPreferences preferences) : IPreferencesService
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
