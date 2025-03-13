using FluentAssertions.Equivalency;
using FluentAssertions.Execution;
using MiniLcm.Tests.AutoFakerHelpers;
using Soenneker.Utils.AutoBogus;
using Soenneker.Utils.AutoBogus.Config;

namespace LcmCrdt.Tests;

public class EntityCopyMethodTests
{
    private static readonly AutoFaker AutoFaker = new(new AutoFakerConfig()
    {
        RepeatCount = 5,
        Overrides =
        [
            new MultiStringOverride(),
            new RichMultiStringOverride(),
            new WritingSystemIdOverride(),
            new OrderableOverride(),
        ],
    });

    public static IEnumerable<object[]> GetEntityTypes()
    {
        var crdtConfig = new CrdtConfig();
        LcmCrdtKernel.ConfigureCrdt(crdtConfig);
        return crdtConfig.ObjectTypes.Select(t => new object[] { t });
    }

    private void AssertDeepCopy(IObjectWithId copy, IObjectWithId original)
    {
        copy.Should().BeEquivalentTo(original, options => options.IncludingAllRuntimeProperties().Using(new NotSameEquivalencyStep()));
    }

    [Fact]
    public void AssertDeepCopy_FailsForShallowCopy()
    {
        var entry = new Entry();
        var copy = entry.Copy();
        copy.LexemeForm = entry.LexemeForm;
        var act = () =>
        {
            AssertDeepCopy(copy, entry);
        };
        act.Should().Throw<Exception>();
    }

    [Theory]
    [MemberData(nameof(GetEntityTypes))]
    public void EntityCopyMethodShouldCopyAllFields(Type type)
    {
        type.IsAssignableTo(typeof(IObjectWithId)).Should().BeTrue();
        var entity = (IObjectWithId) AutoFaker.Generate(type);
        var copy = entity.Copy();
        AssertDeepCopy(copy, entity);
    }

    private class NotSameEquivalencyStep : IEquivalencyStep
    {
        public EquivalencyResult Handle(Comparands comparands,
            IEquivalencyValidationContext context,
            IValidateChildNodeEquivalency valueChildNodes)
        {
            if (comparands.Subject is null || comparands.Expectation is null)
            {
                // I guess both being null is okay.
                return EquivalencyResult.ContinueWithNext;
            }

            if (comparands.RuntimeType.IsValueType || comparands.RuntimeType == typeof(string))
            {
                return EquivalencyResult.ContinueWithNext;
            }

            var assertionChain = AssertionChain.GetOrCreate();

            assertionChain
                .ForCondition(!ReferenceEquals(comparands.Subject, comparands.Expectation))
                .BecauseOf(context.Reason)
                .FailWith("Subject and Expectation for {0} should not reference the same instance in memory.", context.CurrentNode.Description);

            return EquivalencyResult.ContinueWithNext;
        }
    }

}
