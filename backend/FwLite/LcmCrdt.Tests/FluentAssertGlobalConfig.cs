using FluentAssertions.Extensibility;

[assembly: AssertionEngineInitializer(typeof(FluentAssertGlobalConfig), nameof(FluentAssertGlobalConfig.Initialize))]

namespace LcmCrdt.Tests;

public static class FluentAssertGlobalConfig
{
    public static void Initialize()
    {
        MiniLcm.Tests.FluentAssertGlobalConfig.Initialize();
    }
}
