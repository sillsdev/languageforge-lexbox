using System.Reflection;
using System.Text.Json;
using FluentAssertions.Execution;
using LcmCrdt.Changes;
using LcmCrdt.Changes.Entries;
using MiniLcm.Tests.AutoFakerHelpers;
using SIL.Harmony.Changes;
using Soenneker.Utils.AutoBogus;
using SystemTextJsonPatch;

namespace LcmCrdt.Tests.Changes;

public class ChangeSerializationTests
{
    private static readonly AutoFaker Faker = new()
    {
        Config =
        {
            Overrides = [new WritingSystemIdOverride()]
        }
    };

    public static IEnumerable<object[]> Changes()
    {
        foreach (var type in LcmCrdtKernel.AllChangeTypes())
        {
            //can't generate this type because there's no public constructor, so its specified below
            if (type == typeof(SetComplexFormComponentChange)) continue;

            object change;
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(JsonPatchChange<>))
            {
                change = PatchMethod.MakeGenericMethod(type.GenericTypeArguments[0]).Invoke(null, null)!;
            }
            else
            {
                change = Faker.Generate(type);
            }
            change.Should().NotBeNull($"change type {type.Name} should have been generated");
            yield return [change];
        }
        yield return [SetComplexFormComponentChange.NewComplexForm(Guid.NewGuid(), Guid.NewGuid())];
        yield return [SetComplexFormComponentChange.NewComponent(Guid.NewGuid(), Guid.NewGuid())];
        yield return [SetComplexFormComponentChange.NewComponentSense(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid())];
    }

    private static readonly MethodInfo PatchMethod = new Func<IChange>(Patch<Entry>).Method.GetGenericMethodDefinition();

    private static IChange Patch<T>() where T : class
    {
        return new JsonPatchChange<T>(Guid.NewGuid(), new JsonPatchDocument<T>());
    }

    [Theory]
    [MemberData(nameof(Changes))]
    public void CanRoundTripChanges(IChange change)
    {
        var config = new CrdtConfig();
        LcmCrdtKernel.ConfigureCrdt(config);
        //commit id is not serialized
        change.CommitId = Guid.Empty;
        var type = change.GetType();
        var json = JsonSerializer.Serialize(change, config.JsonSerializerOptions);
        var newChange = JsonSerializer.Deserialize(json, type, config.JsonSerializerOptions);
        newChange.Should().BeEquivalentTo(change);
    }

    [Fact]
    public void ChangesIncludesAllValidChangeTypes()
    {
        var allChangeTypes = LcmCrdtKernel.AllChangeTypes();
        allChangeTypes.Should().NotBeEmpty();
        var testedTypes = Changes().Select(c => c[0].GetType()).ToArray();
        using (new AssertionScope())
        {
            foreach (var allChangeType in allChangeTypes)
            {
                testedTypes.Should().Contain(allChangeType);
            }
        }
    }
}
