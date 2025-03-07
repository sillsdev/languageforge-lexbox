using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using FluentAssertions.Execution;
using LcmCrdt.Changes;
using LcmCrdt.Changes.Entries;
using MiniLcm.Tests.AutoFakerHelpers;
using SIL.Harmony.Changes;
using SIL.WritingSystems;
using Soenneker.Utils.AutoBogus;
using SystemTextJsonPatch;

namespace LcmCrdt.Tests.Changes;

public class ChangeSerializationTests
{
    private static readonly Lazy<JsonSerializerOptions> LazyOptions = new(() =>
    {
        var config = new CrdtConfig();
        LcmCrdtKernel.ConfigureCrdt(config);
        config.JsonSerializerOptions.ReadCommentHandling = JsonCommentHandling.Skip;
        return config.JsonSerializerOptions;
    });
    private static readonly JsonSerializerOptions Options = LazyOptions.Value;
    private static readonly AutoFaker Faker = new()
    {
        Config =
        {
            Overrides = [new WritingSystemIdOverride(), new MultiStringOverride()]
        }
    };

    private static IEnumerable<IChange> GeneratedChanges()
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
                try
                {
                    change = Faker.Generate(type);
                }
                catch (Exception e)
                {
                    throw new Exception($"Failed to generate change of type {type.Name}", e);
                }
            }

            change.Should().NotBeNull($"change type {type.Name} should have been generated").And.BeAssignableTo<IChange>();
            yield return (IChange) change;
        }

        yield return SetComplexFormComponentChange.NewComplexForm(Guid.NewGuid(), Guid.NewGuid());
        yield return SetComplexFormComponentChange.NewComponent(Guid.NewGuid(), Guid.NewGuid());
        yield return SetComplexFormComponentChange.NewComponentSense(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
    }

    public static IEnumerable<object[]> Changes()
    {
        foreach (var change in GeneratedChanges())
        {
            yield return [change];
        }
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
        //commit id is not serialized
        change.CommitId = Guid.Empty;
        var type = change.GetType();
        var json = JsonSerializer.Serialize(change, Options);
        var newChange = JsonSerializer.Deserialize(json, type, Options);
        newChange.Should().BeEquivalentTo(change);
    }

    [Fact]
    public void ChangesIncludesAllValidChangeTypes()
    {
        var allChangeTypes = LcmCrdtKernel.AllChangeTypes().ToArray();
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

    [Fact]
    public void CanDeserializeRegressionData()
    {
        //this file represents projects which already have changes applied, we want to ensure that we don't break anything.
        //nothing should ever be removed from this file
        //if a new property is added then a new json object should be added with that property
        using var jsonFile = File.OpenRead(GetJsonFilePath("RegressionDeserializationData.json"));
        var changes = JsonSerializer.Deserialize<List<IChange>>(jsonFile, Options);
        changes.Should().NotBeNullOrEmpty().And.NotContainNulls();

        //ensure that all change types are represented and none should be removed from AllChangeTypes
        using (new AssertionScope())
        {
            var changesSet = changes.Select(c => c.GetType()).Distinct().ToHashSet();
            foreach (var changeType in LcmCrdtKernel.AllChangeTypes())
            {
                changesSet.Should().Contain(changeType);
            }
        }
    }

    //helper method, can be called manually to regenerate the json file
    [Fact(Skip = "Only run manually")]
    public static void GenerateNewJsonFile()
    {
        using var jsonFile = File.Open(GetJsonFilePath("NewJson.json"), FileMode.Create);
        JsonSerializer.Serialize(jsonFile, GeneratedChanges(), Options);
    }

    private static string GetJsonFilePath(string name, [CallerFilePath] string sourceFile = "")
    {
        return Path.Combine(
            Path.GetDirectoryName(sourceFile) ?? throw new InvalidOperationException("Could not get directory of source file"),
            name);
    }
}
