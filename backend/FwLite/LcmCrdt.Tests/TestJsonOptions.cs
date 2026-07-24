using SIL.Harmony.Config;
using System.Text.Json;

namespace LcmCrdt.Tests;

public static class TestJsonOptions
{
    public static JsonSerializerOptions External()
    {
        var config = new HarmonyConfig();
        LcmCrdtKernel.ConfigureCrdt(config);
        // Full external options (resolver + Harmony's IChange converter), matching what the sync client,
        // web host, and debugger use — a resolver alone can't deserialize IChange.
        return config.MakeLcmCrdtExternalJsonOptions();
    }

    public static JsonSerializerOptions Harmony()
    {
        var config = new HarmonyConfig();
        LcmCrdtKernel.ConfigureCrdt(config);
        return config.JsonSerializerOptions;
    }
}
