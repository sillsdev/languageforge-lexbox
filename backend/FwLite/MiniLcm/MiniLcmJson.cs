using System.Text.Json.Serialization.Metadata;
using MiniLcm.Attributes;

namespace MiniLcm;

public static class MiniLcmJson
{
    public static IJsonTypeInfoResolver AddExternalMiniLcmModifiers(this IJsonTypeInfoResolver resolver)
    {
        resolver = resolver.WithAddedModifier(IgnoreInternalMiniLcmProperties);
        return resolver;
    }

    /// <summary>
    /// For the MiniLcm web API: internal properties are also ignored in request bodies, so the API's OpenAPI schema
    /// leaves them out. The sync serializers keep reading them, so they stay on <see cref="AddExternalMiniLcmModifiers"/>.
    /// </summary>
    public static IJsonTypeInfoResolver AddMiniLcmApiModifiers(this IJsonTypeInfoResolver resolver)
    {
        return resolver.AddExternalMiniLcmModifiers().WithAddedModifier(IgnoreInternalMiniLcmPropertiesOnInput);
    }

    private static void IgnoreInternalMiniLcmPropertiesOnInput(JsonTypeInfo typeInfo)
    {
        foreach (var prop in typeInfo.Properties)
        {
            if (prop.AttributeProvider?.IsDefined(typeof(MiniLcmInternalAttribute), inherit: true) ?? false)
            {
                prop.Set = null;
                prop.IsRequired = false;
            }
        }
    }

    private static void IgnoreInternalMiniLcmProperties(JsonTypeInfo typeInfo)
    {
        foreach (var prop in typeInfo.Properties)
        {
            if (prop.AttributeProvider?.IsDefined(typeof(MiniLcmInternalAttribute), inherit: true) ?? false)
            {
                prop.Get = null;//this will prevent even trying to read the property. ComplexFormComponent.Id should not even be read, so we disable it here
                //we probably don't need to set ShouldSerialize anymore, but we'll leave it for now
                prop.ShouldSerialize = (_, _) => false;
            }
        }
    }
}
