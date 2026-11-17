using FwLiteShared.Services;
using LcmCrdt;

namespace FwLiteShared.LocalSync;

/// <summary>
/// Keeps the device's anonymous local author in the device preferences, so the id survives restarts and is
/// shared by every project on the device. The id is created the first time it's needed. The name is whatever
/// the user entered (the viewer writes it to <see cref="PreferenceKey.LocalAuthorName"/>), or null until they do.
/// </summary>
public class PreferencesLocalAuthorProvider(IPreferencesService preferences) : ILocalAuthorProvider
{
    private readonly Lock _lock = new();
    private string? _id;

    public LocalAuthor GetLocalAuthor()
    {
        // not cached: the viewer can change it at any time, and preference reads are in memory
        var name = preferences.Get(nameof(PreferenceKey.LocalAuthorName));
        return new LocalAuthor(GetId(), string.IsNullOrWhiteSpace(name) ? null : name.Trim());
    }

    private string GetId()
    {
        lock (_lock)
        {
            if (_id is not null) return _id;
            _id = preferences.Get(nameof(PreferenceKey.LocalAuthorId));
            if (string.IsNullOrEmpty(_id))
            {
                _id = LocalAuthor.AnonymousIdPrefix + Guid.NewGuid().ToString("N");
                preferences.Set(nameof(PreferenceKey.LocalAuthorId), _id);
            }
            return _id;
        }
    }
}
