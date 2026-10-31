using FwLiteShared.Services;
using LcmCrdt;

namespace FwLiteShared.LocalSync;

/// <summary>
/// Keeps the device's anonymous local author in the device preferences, so the id survives restarts and is
/// shared by every project on the device. The id is created the first time it's needed. The name is whatever the
/// user typed in the viewer (<see cref="PreferenceKey.LocalAuthorName"/>), read on every call so a change applies
/// to the next commit; there is deliberately no default, because a name goes into commits that can't be rewritten.
/// </summary>
public class PreferencesLocalAuthorProvider(IPreferencesService preferences) : ILocalAuthorProvider
{
    private const string IdKey = "localAuthor.id";
    private readonly Lock _lock = new();
    private string? _id;

    public LocalAuthor GetLocalAuthor()
    {
        lock (_lock)
        {
            _id ??= preferences.Get(IdKey) is { Length: > 0 } stored ? stored : NewId();
            var name = preferences.Get(nameof(PreferenceKey.LocalAuthorName));
            return new LocalAuthor(_id, string.IsNullOrWhiteSpace(name) ? null : name.Trim());
        }
    }

    private string NewId()
    {
        var id = LocalAuthor.AnonymousIdPrefix + Guid.NewGuid().ToString("N");
        preferences.Set(IdKey, id);
        return id;
    }
}
