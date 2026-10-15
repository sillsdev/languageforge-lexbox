using FwLiteShared.Services;

namespace FwLiteShared.Tests.AppUpdate;

internal sealed class InMemoryPreferencesService : IPreferencesService
{
    private readonly Dictionary<string, string> _values = new();

    public string? Get(string key) => _values.TryGetValue(key, out var value) ? value : null;

    public void Set(string key, string value) => _values[key] = value;

    public void Remove(string key) => _values.Remove(key);
}
