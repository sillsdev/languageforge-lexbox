using FluentAssertions.Extensibility;
using MiniLcm.Tests;

[assembly: AssertionEngineInitializer(typeof(FluentAssertGlobalConfig), nameof(FluentAssertGlobalConfig.Initialize))]

namespace MiniLcm.Tests;

public static class FluentAssertGlobalConfig
{
    public static void Initialize()
    {
        AssertionConfiguration.Current.Equivalency.Modify(options => options
            //by default, assertion will use the overriden equality function
            //however that will result in very poor error messages, so we override it
            .ComparingByMembers<RichString>()
            .ComparingByMembers<RichSpan>()
            .Excluding(m => (m.DeclaringType == typeof(ComplexFormComponent) || m.DeclaringType == typeof(WritingSystem))
                            && (m.Name == nameof(ComplexFormComponent.Id) || m.Name == nameof(ComplexFormComponent.MaybeId)))
            // Read-only projections of the referenced entries' homograph numbers, which the APIs renumber as
            // homographs come and go, so an expected object built before that goes stale.
            // Asserted directly in ComplexFormComponentTestsBase instead.
            .Excluding(m => m.DeclaringType == typeof(ComplexFormComponent)
                            && (m.Name == nameof(ComplexFormComponent.ComplexFormHomographNumber) || m.Name == nameof(ComplexFormComponent.ComponentHomographNumber)))
            );
    }
}
