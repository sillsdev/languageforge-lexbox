using System.Buffers;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions.Execution;
using LcmCrdt.Changes;
using LcmCrdt.Changes.Entries;
using LcmCrdt.Tests.Data;
using SIL.Harmony.Changes;
using SystemTextJsonPatch;

namespace LcmCrdt.Tests.Changes;

public class ChangeSerializationTests : BaseSerializationTest
{
    private static IEnumerable<IChange> GeneratedChangesForType(Type type)
    {
        //can't generate this type because there's no public constructor
        if (type == typeof(SetComplexFormComponentChange))
        {
            yield return SetComplexFormComponentChange.NewComplexForm(Guid.NewGuid(), Guid.NewGuid());
            yield return SetComplexFormComponentChange.NewComponent(Guid.NewGuid(), Guid.NewGuid());
            yield return SetComplexFormComponentChange.NewComponentSense(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            yield break;
        }

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
        yield return (IChange)change;
    }

    private static IEnumerable<IChange> GeneratedChanges()
    {
        foreach (var type in LcmCrdtKernel.AllChangeTypes())
        {
            foreach (var change in GeneratedChangesForType(type))
            {
                yield return change;
            }
        }
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
    public void CanDeserializeLatestRegressionData()
    {
        //nothing should ever be removed from this file except by moving it to the legacy file!
        //it represents changes that could be out in the wild and we need to support
        //changes are updated and appended by RegressionDataUpToDate() whenever it finds a "latest" change that doesn't stably round-trip
        //or when it finds a change type that isn't represented
        using var jsonFile = File.OpenRead(GetJsonFilePath("ChangeDeserializationRegressionData.latest.verified.txt"));
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

    [Fact]
    public void CanDeserializeLegacyRegressionData()
    {
        //nothing should ever be removed from this file!
        //it represents changes that could be out in the wild and we need to support
        //changes are moved here from ChangeDeserializationRegressionData.latest.verified.txt
        //when RegressionDataUpToDate() detects that it doesn't stably round-trip
        using var jsonFile = File.OpenRead(GetJsonFilePath("ChangeDeserializationRegressionData.legacy.verified.txt"));
        var changes = JsonSerializer.Deserialize<List<IChange>>(jsonFile, Options);
        changes.Should().NotBeNullOrEmpty().And.NotContainNulls();
    }

    [Fact]
    public async Task RegressionDataUpToDate()
    {
        var legacyJsonArray = ReadJsonArrayFromFile(GetJsonFilePath("ChangeDeserializationRegressionData.legacy.verified.txt"));
        var latestJsonArray = ReadJsonArrayFromFile(GetJsonFilePath("ChangeDeserializationRegressionData.latest.verified.txt"));
        var newLatestJsonArray = new JsonArray();

        var seenChangeTypes = new HashSet<Type>();
        foreach (var latestJsonNode in latestJsonArray)
        {
            latestJsonNode.Should().NotBeNull();
            var latestJson = ToNormalizedIndentedJsonString(latestJsonNode);
            latestJson.Should().NotBeNullOrWhiteSpace();
            var change = JsonSerializer.Deserialize<IChange>(latestJsonNode, Options);
            change.Should().NotBeNull();
            seenChangeTypes.Add(change.GetType());
            var newLatestJson = JsonSerializer.Serialize(change, OptionsIndented);

            if (latestJson != newLatestJson)
            {
                // The current "latest" json doesn't match it's reserialized form.
                // I.e. it's no longer the latest. It's now legacy
                legacyJsonArray.Add(latestJsonNode.DeepClone());
                newLatestJsonArray.Add(JsonNode.Parse(newLatestJson));
                // Additionally we add brand new generated changes of the type.
                // If the new model only changes the representation of the same data then this might not be helpful.
                // However, we typically change the model in order to add new data, so the generated change will exercise that new data.
                // Anyhow, it's much easier for a dev to remove unwanted changes than
                // to generate and insert them manually. We can remove this if it's too noisy.
                foreach (var generatedChange in GeneratedChangesForType(change.GetType()))
                {
                    var serialized = JsonSerializer.Serialize(generatedChange, OptionsIndented);
                    newLatestJsonArray.Add(JsonNode.Parse(serialized));
                }
            }
            else
            {
                // it's still the latest
                newLatestJsonArray.Add(latestJsonNode.DeepClone());
            }
        }

        // add changes for any change types not already represented
        foreach (var changeType in LcmCrdtKernel.AllChangeTypes()
            .Where(changeType => !seenChangeTypes.Contains(changeType)))
        {
            foreach (var generatedChange in GeneratedChangesForType(changeType))
            {
                var serialized = JsonSerializer.Serialize(generatedChange, OptionsIndented);
                newLatestJsonArray.Add(JsonNode.Parse(serialized));
            }
        }

        await Task.WhenAll(
            Verify(SerializeRegressionData(legacyJsonArray))
                .UseStrictJson()
                .UseFileName("ChangeDeserializationRegressionData.legacy"),
            Verify(SerializeRegressionData(newLatestJsonArray))
                .UseStrictJson()
                .UseFileName("ChangeDeserializationRegressionData.latest")
        );
    }

    //helper method, can be called manually to regenerate the json file
    //Note: RegressionDataUpToDate() should generate new changes as necessary
    [Fact(Skip = "Only run manually")]
    public static void GenerateNewJsonFile()
    {
        using var jsonFile = File.Open(GetJsonFilePath("NewJson.json"), FileMode.Create);
        JsonSerializer.Serialize(jsonFile, GeneratedChanges(), OptionsIndented);
    }
}
