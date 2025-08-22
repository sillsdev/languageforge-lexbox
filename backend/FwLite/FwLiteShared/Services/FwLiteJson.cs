using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.JSInterop;
using MiniLcm;
using SIL.Harmony;

namespace FwLiteShared.Services;

public static class FwLiteJson
{
    private static readonly PropertyInfo? _jsRuntimeJsonOptionsProperty =
        typeof(JSRuntime).GetProperty("JsonSerializerOptions", BindingFlags.NonPublic | BindingFlags.Instance);

    public static void ConfigureJsonSerializerOptions(this IJSRuntime jsRuntime, CrdtConfig crdtConfig)
    {
        //this is not supported, see: https://github.com/dotnet/aspnetcore/issues/12685
        //doing it anyway, otherwise our serialization will be broken
        if (_jsRuntimeJsonOptionsProperty is null)
            throw new InvalidOperationException("Unable to find JsonSerializerOptions property");
        var options = (JsonSerializerOptions?)_jsRuntimeJsonOptionsProperty.GetValue(jsRuntime, null);
        if (options is null) throw new InvalidOperationException("JSRuntime.JsonSerializerOptions returned null");
        options.TypeInfoResolver = (options.TypeInfoResolver ?? new DefaultJsonTypeInfoResolver())
            .WithAddedModifier(crdtConfig.MakeJsonTypeModifier())
            .AddExternalMiniLcmModifiers();
    }
}
