using System.Text.Json;

namespace LcmCrdt.Tests;

public static class TestJsonOptions
{
    public static JsonSerializerOptions External()
    {
        var config = new CrdtConfig();
        LcmCrdtKernel.ConfigureCrdt(config);
        return new JsonSerializerOptions(JsonSerializerDefaults.General)
        {
            TypeInfoResolver = config.MakeLcmCrdtExternalJsonTypeResolver(),
        };
    }

    public static JsonSerializerOptions Harmony()
    {
        var config = new CrdtConfig();
        LcmCrdtKernel.ConfigureCrdt(config);
        return config.JsonSerializerOptions;
    }
}
