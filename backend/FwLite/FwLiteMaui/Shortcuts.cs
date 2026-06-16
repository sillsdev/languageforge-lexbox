using System.Collections.ObjectModel;

namespace FwLiteMaui;

public static class Shortcuts
{
    // Define shortcut IDs here
    public const string Home = "home";
    public const string ShareLogOut = "share-log-out";

    // Map IDs to URL paths
    private static readonly IReadOnlyDictionary<string, string> IdToUrl = new Dictionary<string, string>
    {
        [Home] = "/",
    };

    // Titles/subtitles shown in the system UI (if supported)
    public static readonly IReadOnlyList<AppAction> Declarations =
    [
        new(Home, "Home"),
        new(ShareLogOut, "Share Debug Log"),
    ];

    public static bool TryGetUrl(string? id, out string url)
    {
        if (id is null)
        {
            url = string.Empty;
            return false;
        }
        if (IdToUrl.TryGetValue(id, out var found))
        {
            url = found;
            return true;
        }
        url = string.Empty;
        return false;
    }
}
