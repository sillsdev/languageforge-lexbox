using FwLiteShared.Services;
using LcmCrdt;

namespace FwLiteShared.LocalSync;

/// <summary>
/// Keeps the device's anonymous local author in the device preferences, so the id survives restarts and is
/// shared by every project on the device. The id is created the first time it's needed.
/// </summary>
/// <param name="defaultName">Name used until the user picks one. Hosts that know a better device name pass it in:
/// <see cref="Environment.MachineName"/> is "localhost" on Android.</param>
public class PreferencesLocalAuthorProvider(IPreferencesService preferences, string? defaultName = null) : ILocalAuthorProvider
{
    private const string IdKey = "localAuthor.id";
    private const string NameKey = "localAuthor.name";
    private readonly Lock _lock = new();
    private LocalAuthor? _localAuthor;

    // Called once per commit, so read the preferences once and keep the result.
    public LocalAuthor GetLocalAuthor()
    {
        lock (_lock)
        {
            if (_localAuthor is not null) return _localAuthor;
            var id = preferences.Get(IdKey);
            if (string.IsNullOrEmpty(id))
            {
                id = LocalAuthor.AnonymousIdPrefix + Guid.NewGuid().ToString("N");
                preferences.Set(IdKey, id);
            }

            var name = preferences.Get(NameKey);
            _localAuthor = new LocalAuthor(id, string.IsNullOrWhiteSpace(name) ? defaultName ?? Environment.MachineName : name);
            return _localAuthor;
        }
    }
}
