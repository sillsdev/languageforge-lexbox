using System.Runtime.CompilerServices;
using FluentAssertions.Extensibility;
using FwLiteProjectSync.Tests;

[assembly: AssertionEngineInitializer(typeof(FluentAssertGlobalConfig), nameof(FluentAssertGlobalConfig.Initialize))]

namespace FwLiteProjectSync.Tests;

public static class FluentAssertGlobalConfig
{
    [ModuleInitializer]
    internal static void InitVerify()
    {
        VerifierSettings.OmitContentFromException();
    }

    public static void Initialize()
    {
        MiniLcm.Tests.FluentAssertGlobalConfig.Initialize();
    }
}
