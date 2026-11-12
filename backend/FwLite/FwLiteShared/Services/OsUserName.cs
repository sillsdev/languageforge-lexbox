using System.Runtime.InteropServices;

namespace FwLiteShared.Services;

public static class OsUserName
{
    /// <summary>
    /// Prefill for the viewer's name prompt; never used without the user confirming it.
    /// </summary>
    public static string? Suggest()
    {
        if (!OperatingSystem.IsWindows()) return null;
        var name = DisplayName() ?? Environment.UserName;
        var at = name.IndexOf('@');
        if (at >= 0) name = name[..at];
        return string.IsNullOrWhiteSpace(name) ? null : name.Trim();
    }

    private const int NameDisplay = 3;

    private static string? DisplayName()
    {
        var buffer = new char[256];
        var size = (uint)buffer.Length;
        return GetUserNameEx(NameDisplay, buffer, ref size) && size > 0 ? new string(buffer, 0, (int)size) : null;
    }

    [DllImport("secur32.dll", EntryPoint = "GetUserNameExW", CharSet = CharSet.Unicode)]
    private static extern bool GetUserNameEx(int nameFormat, [Out] char[] buffer, ref uint size);
}
