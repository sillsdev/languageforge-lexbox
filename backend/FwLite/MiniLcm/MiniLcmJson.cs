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
