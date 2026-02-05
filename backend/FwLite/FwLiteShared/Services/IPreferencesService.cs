namespace FwLiteShared.Services;

/// <summary>
/// Interface for a key-value preferences service.
/// Used for storing user preferences, exposed to JavaScript via JSInterop.
/// </summary>
public interface IPreferencesService
{
    string? Get(string key);
    void Set(string key, string value);
    void Remove(string key);
}
