using System.Runtime.InteropServices;

namespace FwLiteShared.Services;

public static class RuntimeInfoHelper
{
    // Android ships a separate bundle per ABI (CoreCLR/64-bit vs Mono/armeabi-v7a), so the
    // process architecture is a ground-truth fingerprint of which bundle a device installed.
    public static string ProcessArchitecture()
    {
        var bits = Environment.Is64BitProcess ? "64-bit" : "32-bit";
        return $"{RuntimeInformation.ProcessArchitecture} ({bits})";
    }
}
