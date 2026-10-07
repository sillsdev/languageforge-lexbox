using System.Text.Json;
using FluentAssertions.Execution;
using LcmCrdt.Tests.Data;
using SIL.Harmony.Changes;

namespace LcmCrdt.Tests.Changes;

public class ChangeSerializationTests : BaseSerializationTest
{
    private static IChange GenerateChangeForType(Type type)
    {
        object change;
        try
        {
            change = Faker.Generate(type);
        }
        catch (Exception e)
        {
            throw new Exception($"Failed to generate change of type {type.Name}", e);
        }

        change.Should().NotBeNull($"change type {type.Name} should have been generated").And.BeAssignableTo<IChange>();
        return (IChange)change;
    }

    private static IEnumerable<IChange> GeneratedChanges()
    {
        return LcmCrdtKernel.AllChangeTypes().Select(GenerateChangeForType);
    }

    public static IEnumerable<object[]> Changes()
    {
        foreach (var change in GeneratedChanges())
        {
            yield return [change];
        }
    }

    [Theory]
    [MemberData(nameof(Changes))]
    public void CanRoundTripChanges(IChange change)
    {
        var type = change.GetType();
        var json = JsonSerializer.Serialize(change, HarmonyJsonOptions);
        var newChange = JsonSerializer.Deserialize(json, type, HarmonyJsonOptions);
        newChange.Should().BeEquivalentTo(change);
    }

    [Theory]
    [MemberData(nameof(Changes))]
    public void CanRoundTripChangesAsIChangeWithExternalOptions(IChange change)
    {
        // The sync client, web host, and debugger all (de)serialize change fields as the abstract IChange
        // using the external (Web) options. That relies on Harmony's polymorphic converter, not just the
        // type resolver — regression guard against the "Deserialization of interface or abstract types is
        // not supported. Type 'IChange'" break when only the resolver was wired onto the external options.
        var options = TestJsonOptions.External();
        var json = JsonSerializer.Serialize(change, options);
        var newChange = JsonSerializer.Deserialize<IChange>(json, options);
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
        var changes = JsonSerializer.Deserialize<List<IChange>>(jsonFile, HarmonyJsonOptions);
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
        //the input fields represent changes that could be out in the wild and we need to support
        //the output fields represent what these legacy changes currently "reserialize" to
        //RegressionDataUpToDate()
        // (1) moves changes here from RegressionDeserializationData.latest.verified.txt
        // when it detects that they don't stably round-trip and
        // (2) keeps the round-trip output of the changes up to date
        using var jsonFile = File.OpenRead(GetJsonFilePath("ChangeDeserializationRegressionData.legacy.verified.txt"));
        var changes = JsonSerializer.Deserialize<List<LegacyRecord<IChange>>>(jsonFile, HarmonyJsonOptions);
        changes.Should().NotBeNullOrEmpty().And.NotContainNulls();
        changes.SelectMany(c => new[] { c.Input, c.Output })
            .Should().NotContainNulls()
            .And.HaveCount(changes.Count * 2);
    }

    [Fact]
    [Trait("Category", "Verified")]
    public Task RegressionDataUpToDate()
    {
        return VerifyRegressionDataUpToDate<IChange>(
            "ChangeDeserializationRegressionData",
            LcmCrdtKernel.AllChangeTypes(),
            change => change.GetType(),
            GenerateChangeForType);
    }

    //helper method, can be called manually to regenerate the json file
    //Note: RegressionDataUpToDate() should generate new changes as necessary
    [Fact(Skip = "Only run manually")]
    public static void GenerateNewJsonFile()
    {
        using var jsonFile = File.Open(GetJsonFilePath("NewJson.json"), FileMode.Create);
        JsonSerializer.Serialize(jsonFile, GeneratedChanges(), IndentedHarmonyJsonOptions);
    }
}
