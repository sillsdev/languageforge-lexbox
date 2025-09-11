using System.Text.Json;

namespace LcmCrdt.Tests;

public static class TestJsonOptions
{
    public static JsonSerializerOptions Default(bool ignoreInternal = true)
    {
        var config = new CrdtConfig();
        LcmCrdtKernel.ConfigureCrdt(config);
        return new JsonSerializerOptions(JsonSerializerDefaults.General)
        {
            TypeInfoResolver =
                config.MakeLcmCrdtExternalJsonTypeResolver(ignoreInternal)
        };
    }
}
