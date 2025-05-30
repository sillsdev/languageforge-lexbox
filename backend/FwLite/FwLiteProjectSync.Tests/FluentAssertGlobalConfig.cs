using FluentAssertions.Extensibility;
using FwLiteProjectSync.Tests;

[assembly: AssertionEngineInitializer(typeof(FluentAssertGlobalConfig), nameof(FluentAssertGlobalConfig.Initialize))]

namespace FwLiteProjectSync.Tests;

public static class FluentAssertGlobalConfig
{
    public static void Initialize()
    {
        MiniLcm.Tests.FluentAssertGlobalConfig.Initialize();
    }
}
