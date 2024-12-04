using SIL.Harmony.Entities;
using LcmCrdt.Objects;
using MiniLcm.Tests.AutoFakerHelpers;
using SIL.Harmony;
using Soenneker.Utils.AutoBogus;
using Soenneker.Utils.AutoBogus.Config;

namespace LcmCrdt.Tests;

public class EntityCopyMethodTests
{
    private static readonly AutoFaker AutoFaker = new(new AutoFakerConfig()
    {
        Overrides = [new MultiStringOverride(), new WritingSystemIdOverride()]
    });

    public static IEnumerable<object[]> GetEntityTypes()
    {
        var crdtConfig = new CrdtConfig();
        LcmCrdtKernel.ConfigureCrdt(crdtConfig);
        Type[] types = [
            //todo get via reflection or from crdt config
            typeof(Entry),
            typeof(Sense),
            typeof(ExampleSentence),
            typeof(WritingSystem),
            typeof(PartOfSpeech),
            typeof(SemanticDomain),
            typeof(ComplexFormType)
        ];
        return types.Select(t => new object[] { t });
    }

    [Theory]
    [MemberData(nameof(GetEntityTypes))]
    public void EntityCopyMethodShouldCopyAllFields(Type type)
    {
        type.IsAssignableTo(typeof(IObjectWithId)).Should().BeTrue();
        var entity = (IObjectWithId) AutoFaker.Generate(type);
        var copy = entity.Copy();
        //todo this does not detect a deep copy, but it should as that breaks stuff
        copy.Should().BeEquivalentTo(entity, options => options.IncludingAllRuntimeProperties());
    }
}
