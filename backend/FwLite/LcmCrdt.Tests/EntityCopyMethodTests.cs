﻿using SIL.Harmony.Entities;
using LcmCrdt.Objects;
using MiniLcm.Tests.AutoFakerHelpers;
using SIL.Harmony;
using Soenneker.Utils.AutoBogus;
using Soenneker.Utils.AutoBogus.Config;

namespace LcmCrdt.Tests;

public class EntityCopyMethodTests
{
    private readonly AutoFaker _autoFaker = new(new AutoFakerConfig()
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
            typeof(SemanticDomain)
        ];
        return types.Select(t => new object[] { t });
    }

    [Theory]
    [MemberData(nameof(GetEntityTypes))]
    public void EntityCopyMethodShouldCopyAllFields(Type type)
    {
        type.IsAssignableTo(typeof(IObjectWithId)).Should().BeTrue();
        var entity = (IObjectWithId) _autoFaker.Generate(type);
        var copy = entity.Copy();
        copy.Should().BeEquivalentTo(entity, options => options.IncludingAllRuntimeProperties());
    }
}
