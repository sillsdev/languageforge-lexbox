using SIL.Harmony.Config;
using System.Text.Json;

namespace LcmCrdt.Tests;

public static class TestJsonOptions
{
    public static JsonSerializerOptions Sync()
    {
        var config = new HarmonyConfig();
        LcmCrdtKernel.ConfigureCrdt(config);
        // Full sync options (resolver + Harmony's IChange converter), matching what the sync client
        // and debugger use — a resolver alone can't deserialize IChange.
        return config.MakeLcmCrdtSyncJsonOptions();
    }

    public static JsonSerializerOptions Harmony()
    {
        var config = new HarmonyConfig();
        LcmCrdtKernel.ConfigureCrdt(config);
        return config.JsonSerializerOptions;
    }
}
