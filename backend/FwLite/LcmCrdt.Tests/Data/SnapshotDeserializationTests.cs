using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions.Execution;
using LcmCrdt.Objects;
using SIL.Harmony.Core;

namespace LcmCrdt.Tests.Data;

public class SnapshotDeserializationTests : BaseSerializationTest
{

    private static IObjectBase GenerateSnapshotForType(Type type)
    {
        try
        {
            if (typeof(IObjectWithId).IsAssignableFrom(type))
            {
                var snapshot = Faker.Generate(type) as IObjectWithId;
                snapshot.Should().NotBeNull($"Could not generate {nameof(IObjectWithId)} snapshot for type {type.Name}");
                return new MiniLcmCrdtAdapter(snapshot);
            }
            else if (typeof(IObjectBase).IsAssignableFrom(type))
            {
                var snapshot = Faker.Generate(type) as IObjectBase;
                snapshot.Should().NotBeNull($"Could not generate {nameof(IObjectBase)} snapshot for type {type.Name}");
                return snapshot;
            }
            else
            {
                throw new Exception($"Type {type.Name} does not implement IObjectBase or IObjectWithId");
            }

        }
        catch (Exception e)
        {
            throw new Exception($"Failed to generate snapshot of type {type.Name}", e);
        }
    }

    [Fact]
    public void CanDeserializeProjectDumpRegressionData()
    {
        //nothing should ever be removed from this file
        //this file represents projects which already have snapshots and changes applied, we want to ensure that we don't break anything.
        using var jsonFile = File.OpenRead(GetJsonFilePath("SnapshotDeserializationRegressionData.ProjectDump.1.json"));
        var snapshots = JsonSerializer.Deserialize<List<IObjectBase>>(jsonFile, HarmonyJsonOptions);
        snapshots.Should().NotBeNullOrEmpty().And.NotContainNulls();
    }

    [Fact]
    public void CanDeserializeLatestRegressionData()
    {
        //nothing should ever be removed from this file except by moving it to the legacy file!
        //it represents snapshots that could be out in the wild and we need to support
        //snapshots are updated and appended by RegressionDataUpToDate() whenever it finds a "latest" snapshot that doesn't stably round-trip
        //or when it finds a snapshot type that isn't represented
        //this file was initialized with a small selection of snapshots from a project dump
        using var jsonFile = File.OpenRead(GetJsonFilePath("SnapshotDeserializationRegressionData.latest.verified.txt"));
        var snapshots = JsonSerializer.Deserialize<List<IObjectBase>>(jsonFile, HarmonyJsonOptions);
        snapshots.Should().NotBeNullOrEmpty().And.NotContainNulls();

        //ensure that all snapshot types are represented and none should be removed from AllObjectTypes
        using (new AssertionScope())
        {
            var snapshotTypes = snapshots.Select(c => c.DbObject.GetType()).Distinct().ToHashSet();
            foreach (var objectType in LcmCrdtKernel.AllObjectTypes())
            {
                snapshotTypes.Should().Contain(objectType);
            }
        }
    }

    private record LegacySnapshotRecord(IObjectBase Input, IObjectBase Output);

    [Fact]
    public void CanDeserializeLegacyRegressionData()
    {
        //nothing should ever be removed from this file!
        //the input fields represent snapshots that could be out in the wild and we need to support
        //the output fields represent what these legacy snapshots currently "reserialize" to
        //RegressionDataUpToDate()
        // (1) moves snapshots here from RegressionDeserializationData.latest.verified.txt
        // when it detects that they don't stably round-trip and
        // (2) keeps the round-trip output of the snapshots up to date
        using var jsonFile = File.OpenRead(GetJsonFilePath("SnapshotDeserializationRegressionData.legacy.verified.txt"));
        var snapshots = JsonSerializer.Deserialize<List<LegacySnapshotRecord>>(jsonFile, HarmonyJsonOptions);
        snapshots.Should().NotBeNullOrEmpty().And.NotContainNulls();
        snapshots.SelectMany(c => new[] { c.Input, c.Output })
            .Should().NotContainNulls()
            .And.HaveCount(snapshots.Count * 2);
    }

    [Fact]
    public async Task RegressionDataUpToDate()
    {
        var legacyJsonArray = ReadJsonArrayFromFile(GetJsonFilePath("SnapshotDeserializationRegressionData.legacy.verified.txt"));
        var latestJsonArray = ReadJsonArrayFromFile(GetJsonFilePath("SnapshotDeserializationRegressionData.latest.verified.txt"));
        var newLatestJsonArray = new JsonArray();

        // step 1: validate the round-tripping/output of legacy snapshots
        foreach (var legacyJsonNode in legacyJsonArray)
        {
            legacyJsonNode.Should().NotBeNull();
            var legacyJson = ToNormalizedIndentedJsonString(legacyJsonNode[nameof(LegacySnapshotRecord.Input)]!);
            legacyJson.Should().NotBeNullOrWhiteSpace();
            var legacyOutputJson = ToNormalizedIndentedJsonString(legacyJsonNode[nameof(LegacySnapshotRecord.Output)]!);
            legacyOutputJson.Should().NotBeNullOrWhiteSpace();
            var snapshot = JsonSerializer.Deserialize<IObjectBase>(legacyJson, HarmonyJsonOptions);
            snapshot.Should().NotBeNull();
            var newLegacyOutputJson = JsonSerializer.Serialize(snapshot, IndentedHarmonyJsonOptions);
            if (legacyOutputJson != newLegacyOutputJson)
            {
                //the legacy snapshot no longer round-trips to the same output, so we should verify the new output
                legacyJsonNode[nameof(LegacySnapshotRecord.Output)] = JsonNode.Parse(newLegacyOutputJson);
            }
        }

        // step 2: validate the round-tripping/output of latest snapshots, moving any that don't to legacy
        var seenObjectTypes = new HashSet<Type>();
        foreach (var latestJsonNode in latestJsonArray)
        {
            latestJsonNode.Should().NotBeNull();
            var latestJson = ToNormalizedIndentedJsonString(latestJsonNode);
            latestJson.Should().NotBeNullOrWhiteSpace();
            var snapshot = JsonSerializer.Deserialize<IObjectBase>(latestJsonNode, HarmonyJsonOptions);
            snapshot.Should().NotBeNull();
            seenObjectTypes.Add(snapshot.DbObject.GetType());
            var newLatestJson = JsonSerializer.Serialize(snapshot, IndentedHarmonyJsonOptions);

            if (latestJson != newLatestJson)
            {
                // The current "latest" json doesn't match it's reserialized form.
                // I.e. it's no longer the latest. It's now legacy
                legacyJsonArray.Add(new JsonObject
                {
                    [nameof(LegacySnapshotRecord.Input)] = latestJsonNode.DeepClone(),
                    [nameof(LegacySnapshotRecord.Output)] = JsonNode.Parse(newLatestJson)
                });
                newLatestJsonArray.Add(JsonNode.Parse(newLatestJson));
                // Additionally we add a brand new generated snapshot.
                // If the new model only removes data or changes the representation of the same data then this probably isn't helpful.
                // However, we typically change the model in order to add new data, so the generated snapshot will exercise that new data.
                // Anyhow, it's much easier for a dev to remove unwanted snapshots than
                // to generate and insert them manually. We can remove this if it's too noisy.
                var generatedSnapshot = GenerateSnapshotForType(snapshot.DbObject.GetType());
                var serialized = JsonSerializer.Serialize(generatedSnapshot, IndentedHarmonyJsonOptions);
                newLatestJsonArray.Add(JsonNode.Parse(serialized));
            }
            else
            {
                // it's still the latest
                newLatestJsonArray.Add(latestJsonNode.DeepClone());
            }
        }

        // step 3: add snapshots for any snapshot types not already represented
        foreach (var snapshotType in LcmCrdtKernel.AllObjectTypes()
            .Where(snapshotType => !seenObjectTypes.Contains(snapshotType)))
        {
            var generatedSnapshot = GenerateSnapshotForType(snapshotType);
            var serialized = JsonSerializer.Serialize(generatedSnapshot, IndentedHarmonyJsonOptions);
            newLatestJsonArray.Add(JsonNode.Parse(serialized));
        }

        await Task.WhenAll(
            Verify(SerializeRegressionData(legacyJsonArray))
                .UseStrictJson()
                .UseFileName("SnapshotDeserializationRegressionData.legacy"),
            Verify(SerializeRegressionData(newLatestJsonArray))
                .UseStrictJson()
                .UseFileName("SnapshotDeserializationRegressionData.latest")
        );
    }
}
