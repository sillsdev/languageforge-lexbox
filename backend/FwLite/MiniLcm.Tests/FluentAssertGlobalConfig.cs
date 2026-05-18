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
            .Excluding(m => (m.DeclaringType == typeof(ComplexFormComponent) || m.DeclaringType == typeof(WritingSystem))
                            && (m.Name == nameof(ComplexFormComponent.Id) || m.Name == nameof(ComplexFormComponent.MaybeId)))
            );
    }
}
