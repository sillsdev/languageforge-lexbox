using Icu;
using Microsoft.Extensions.Logging;

namespace LcmCrdt.Culture;

public static class IcuInit
{
    // volatile: read outside the lock, and _initError must be visible once this is true
    private static volatile bool _initialized;
    // Set when Init failed, so later callers fail the same way instead of retrying u_init every time
    private static Exception? _initError;
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
        if (!_initialized)
        {
            lock (_lock)
            {
                if (!_initialized)
                {
                    try
                    {
                        var errorCode = Icu.Wrapper.Init();
                        // ICU codes above ZERO_ERROR are failures; negative ones are warnings
                        if (errorCode > ErrorCode.ZERO_ERROR)
                            _initError = new InvalidOperationException($"ICU failed to initialize: {errorCode}");
                    }
                    catch (Exception e)
                    {
                        _initError = e;
                    }

                    _initialized = true;
                }
            }
        }

        if (_initError is not null)
            throw new InvalidOperationException("ICU is not available", _initError);
    }
}
