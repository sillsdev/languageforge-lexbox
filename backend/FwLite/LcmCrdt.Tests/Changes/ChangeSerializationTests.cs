using System.Buffers;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
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
    private static readonly Lazy<JsonSerializerOptions> LazyOptions = new(() =>
    {
        var config = new CrdtConfig();
        LcmCrdtKernel.ConfigureCrdt(config);
        config.JsonSerializerOptions.ReadCommentHandling = JsonCommentHandling.Skip;
        return config.JsonSerializerOptions;
    });
    private static readonly JsonSerializerOptions Options = LazyOptions.Value;
    private static readonly JsonSerializerOptions OptionsIndented = new(Options)
    {
        WriteIndented = true,
    };
    private static readonly AutoFaker Faker = new()
    {
        Config = AutoFakerDefault.MakeConfig(repeatCount: 1)
    };

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
        //nothing should ever be removed from this file!
        //it represents changes that could be out in the wild and we need to support
        //changes are updated and appended by RegressionDataUpToDate() whenever it finds a "latest" change that doesn't stably round-trip
        //or when it finds a change type that isn't represented
        using var jsonFile = File.OpenRead(GetJsonFilePath("RegressionDeserializationData.latest.verified.txt"));
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
        //changes are moved here from RegressionDeserializationData.latest.verified.txt
        //when RegressionDataUpToDate() detects that it doesn't stably round-trip
        using var jsonFile = File.OpenRead(GetJsonFilePath("RegressionDeserializationData.legacy.verified.txt"));
        var changes = JsonSerializer.Deserialize<List<IChange>>(jsonFile, Options);
        changes.Should().NotBeNullOrEmpty().And.NotContainNulls();
    }

    [Fact]
    public async Task RegressionDataUpToDate()
    {
        var legacyJsonArray = ReadJsonArrayFromFile(GetJsonFilePath("RegressionDeserializationData.legacy.verified.txt"));
        var latestJsonArray = ReadJsonArrayFromFile(GetJsonFilePath("RegressionDeserializationData.latest.verified.txt"));
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
                // additionally we re-add generated changes, because it's much easier for a dev to remove unwanted changes than
                // to manually generate and insert them. We can remove this if it's too noisy
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

        await Task.WhenAll(
            Verify(SerializeRegressionData(legacyJsonArray))
                .UseStrictJson()
                .UseFileName("RegressionDeserializationData.legacy"),
            Verify(SerializeRegressionData(newLatestJsonArray))
                .UseStrictJson()
                .UseFileName("RegressionDeserializationData.latest")
        );
    }

    private static string SerializeRegressionData(JsonArray jsonArray)
    {
        return JsonSerializer.Serialize(jsonArray, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = Options.Encoder,
        })
        // The "+" in DateTimeOffsets does not get escaped by our standard crdt serializer,
        // but it does here. Presumably, because it's reading it as a string and not a DateTimeOffset
        .Replace("\\u002B", "+");
    }

    //helper method, can be called manually to regenerate the json file
    //Note: RegressionDataUpToDate() should generate new changes as necessary
    [Fact(Skip = "Only run manually")]
    public static void GenerateNewJsonFile()
    {
        using var jsonFile = File.Open(GetJsonFilePath("NewJson.json"), FileMode.Create);
        JsonSerializer.Serialize(jsonFile, GeneratedChanges(), OptionsIndented);
    }

    private static string GetJsonFilePath(string name, [CallerFilePath] string sourceFile = "")
    {
        return Path.Combine(
            Path.GetDirectoryName(sourceFile) ?? throw new InvalidOperationException("Could not get directory of source file"),
            name);
    }

    private static readonly JsonWriterOptions GenericJsonWriterOptions = new()
    {
        Indented = true,
        Encoder = Options.Encoder,
    };

    private static string ToNormalizedIndentedJsonString(JsonNode element)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer, GenericJsonWriterOptions))
        {
            element.WriteTo(writer);
        }
        return Encoding.UTF8.GetString(buffer.WrittenSpan)
        // The "+" in DateTimeOffsets does not get escaped by our standard crdt serializer,
        // but it does here. Presumably, because it's reading it as a string and not a DateTimeOffset
        .Replace("\\u002B", "+");
    }

    private static JsonArray ReadJsonArrayFromFile(string path)
    {
        using var stream = File.OpenRead(path);
        var node = JsonNode.Parse(stream, null, new()
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip
        }) ?? throw new InvalidOperationException("Could not parse json array");
        return node.AsArray();
    }
}
