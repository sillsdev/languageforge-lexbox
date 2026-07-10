using FluentAssertions.Extensibility;
using MiniLcm.Tests;

[assembly: AssertionEngineInitializer(typeof(FluentAssertGlobalConfig), nameof(FluentAssertGlobalConfig.Initialize))]

namespace MiniLcm.Tests;

public static class FluentAssertGlobalConfig
{
    public static void Initialize()
    {
        AssertionOptions.AssertEquivalencyUsing(options => options
            //by default, assertion will use the overriden equality function
            //however that will result in very poor error messages, so we override it
            .ComparingByMembers<RichString>()
            .ComparingByMembers<RichSpan>()
            //Id/MaybeId are sync-irrelevant on every type that carries the unset-id pattern
            .Excluding(m => (m.DeclaringType == typeof(ComplexFormComponent) || m.DeclaringType == typeof(Variant) || m.DeclaringType == typeof(WritingSystem))
                            && (m.Name == "Id" || m.Name == "MaybeId"))
            //Shadow query-rewrite targets — domain state lives on the underlying collection.
            .Excluding(m => (m.DeclaringType == typeof(Entry) && m.Name == nameof(Entry.PublishInRows))
                            || (m.DeclaringType == typeof(Sense) && m.Name == nameof(Sense.SemanticDomainRows)))
            );
    }
}
