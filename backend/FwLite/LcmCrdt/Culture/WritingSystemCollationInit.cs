namespace LcmCrdt.Culture;

internal static class WritingSystemCollationInit
{
    private static bool _initialized;
    private static readonly Lock _lock = new();

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
