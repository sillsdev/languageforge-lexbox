using System.Runtime.InteropServices;

namespace FwLiteShared.Services;

public static class RuntimeInfoHelper
{
    // On Android the process architecture reveals which bundle a device installed (Mono/arm32 vs CoreCLR/64-bit).
    public static string ProcessArchitecture()
    {
        var bits = Environment.Is64BitProcess ? "64-bit" : "32-bit";
        return $"{RuntimeInformation.ProcessArchitecture} ({bits})";
    }
}
