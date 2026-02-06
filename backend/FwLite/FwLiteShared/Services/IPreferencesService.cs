using Microsoft.JSInterop;
using Reinforced.Typings.Attributes;

namespace FwLiteShared.Services;

/// <summary>
/// Interface for a key-value preferences service.
/// Used for storing user preferences, exposed to JavaScript via JSInterop.
/// </summary>
public interface IPreferencesService
{
    [JSInvokable]
    [TsFunction(Type = "Promise<string | null>")]
    string? Get(string key);
    [JSInvokable]
    void Set(string key, string value);
    [JSInvokable]
    void Remove(string key);
}
