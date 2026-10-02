using Microsoft.Extensions.Logging;

namespace LcmCrdt.Culture;

public static class IcuInit
{
    private static bool _initialized;
    private static readonly Lock _lock = new();

    /// <summary>
    /// Call once at app startup so ICU is set up before anything (e.g. SIL.WritingSystems) touches it.
    /// Failure is logged, not thrown; collation falls back to .NET when ICU is unavailable.
    /// </summary>
    public static bool TryInitialize(ILogger logger)
    {
        try
        {
            EnsureInitialized();
            logger.LogInformation("ICU initialized, version {IcuVersion}", Icu.Wrapper.IcuVersion);
            return true;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to initialize ICU");
            return false;
        }
    }

    internal static void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        lock (_lock)
        {
            if (_initialized)
            {
                return;
            }

            Icu.Wrapper.Init();
            _initialized = true;
        }
    }
}
