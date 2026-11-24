namespace FwLiteMaui;

/// <summary>
/// Command-line options for desktop launches, e.g. <c>FwLiteMaui.exe --start-url /</c> opens the home page instead of
/// the last visited page. Lets scripted launches (QA harnesses, test runs) start from a known page.
/// </summary>
public static class StartupArgs
{
    public const string StartUrlOption = "--start-url";

    public static string? GetStartUrl(IReadOnlyList<string> args)
    {
        for (var i = 0; i < args.Count; i++)
        {
            var arg = args[i];
            string? value = null;
            if (arg == StartUrlOption && i + 1 < args.Count) value = args[i + 1];
            else if (arg.StartsWith(StartUrlOption + "=", StringComparison.Ordinal)) value = arg[(StartUrlOption.Length + 1)..];
            //app-relative paths only, same rule as the saved last url
            if (value?.StartsWith('/') == true) return value;
        }
        return null;
    }
}
