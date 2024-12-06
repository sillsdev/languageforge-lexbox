using System.Text.Json.Serialization.Metadata;
using MiniLcm.Attributes;

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
                prop.ShouldSerialize = (_, _) => false;
            }
        }
    }
}
