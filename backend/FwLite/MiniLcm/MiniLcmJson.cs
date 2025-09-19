using System.Diagnostics;
using System.Text.Json.Serialization.Metadata;
using MiniLcm.Attributes;
using MiniLcm.Models;

namespace MiniLcm;

public static class MiniLcmJson
{
    public static IJsonTypeInfoResolver AddMiniLcmModifiers(this IJsonTypeInfoResolver resolver, bool ignoreInternal = true)
    {
        if (ignoreInternal)
            resolver = resolver.WithAddedModifier(IgnoreInternalMiniLcmProperties);
        resolver = resolver.WithAddedModifier(ExampleTranslationHandling);
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

    private static void ExampleTranslationHandling(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Type == typeof(ExampleSentence))
        {
            //legacy property
            var propertyInfo = typeInfo.CreateJsonPropertyInfo(typeof(RichMultiString), "Translation");
            propertyInfo.Set = (obj, value) =>
            {
                var exampleSentence = (ExampleSentence)obj;
                if (exampleSentence.Translations.Any()) throw new InvalidOperationException("Cannot set translations when they already exist.");
                var richString = (RichMultiString?)value;
                if (richString is null) return;
#pragma warning disable CS0618 // Type or member is obsolete
                exampleSentence.Translations = [Translation.FromMultiString(richString)];
#pragma warning restore CS0618 // Type or member is obsolete
            };
            typeInfo.Properties.Add(propertyInfo);
        }
    }
}
