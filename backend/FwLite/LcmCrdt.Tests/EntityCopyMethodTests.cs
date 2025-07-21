using FluentAssertions.Equivalency;
using FluentAssertions.Execution;
using MiniLcm.Tests.AutoFakerHelpers;
using SIL.Harmony.Resource;
using Soenneker.Utils.AutoBogus;
using Soenneker.Utils.AutoBogus.Config;

namespace LcmCrdt.Tests;

public class EntityCopyMethodTests
{
    private static readonly AutoFaker AutoFaker = new(AutoFakerDefault.Config);

    public static IEnumerable<object[]> GetEntityTypes()
    {
        var crdtConfig = new CrdtConfig();
        LcmCrdtKernel.ConfigureCrdt(crdtConfig);
        return crdtConfig.ObjectTypes
            .Except([typeof(RemoteResource)])//exclude remote resource as it's a harmony defined type, not miniLcm
            .Select(t => new object[] { t });
    }

    private void AssertDeepCopy(object copy, object original)
    {
        copy.Should().BeEquivalentTo(original, options => options
            .ComparingByMembers(typeof(RichString))//ignore built in Equality as it returns true when the same instance
            .ComparingByMembers(typeof(RichSpan))
            .ComparingRecordsByMembers()
            .IncludingAllRuntimeProperties()
            .IncludingFields()
            .Using(new NotSameEquivalencyStep()));
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

    [Fact]
    public void CopyOfRichStringIsDeep()
    {
        var original = new RichString([
            new RichSpan()
            {
                Text = "test",
                ObjData = new RichTextObjectData() { Type = RichTextObjectDataType.NameGuid, Value = "test" },
                Tags = [Guid.NewGuid()]
            }
        ]);
        var copy = original.Copy();
        AssertDeepCopy(copy, original);
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
