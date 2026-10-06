using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using LcmCrdt.Changes;
using MiniLcm.Models;
using MiniLcm.Tests.AutoFakerHelpers;
using Soenneker.Utils.AutoBogus;
using Soenneker.Utils.AutoBogus.Config;

namespace LcmCrdt.Tests.Data;

public abstract class BaseSerializationTest
{
    protected static readonly JsonSerializerOptions HarmonyJsonOptions = new(TestJsonOptions.Harmony())
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
    };
    protected static readonly JsonSerializerOptions IndentedHarmonyJsonOptions = new(HarmonyJsonOptions)
    {
        WriteIndented = true,
    };
    private static AutoFakerConfig GetAutoFakerConfig()
    {
        var config = AutoFakerDefault.MakeConfig(repeatCount: 1, minimalRichSpans: true);
        config.Overrides!.AddRange(
            // We're generating object snapshots, which don't include related entities
            // so we clear them to reflect real snapshots.
            new SimpleOverride<Entry>(context =>
            {
                if (context.Instance is Entry entry)
                {
                    entry.Senses = [];
                    entry.ComplexForms = [];
                    entry.Components = [];
                }
            }, true),
            new SimpleOverride<Sense>(context =>
            {
                if (context.Instance is Sense sense)
                {
                    sense.ExampleSentences = [];
                }
            }, true),
            new SimpleOverride<CreateExampleSentenceChange>(context =>
            {
                if (context.Instance is CreateExampleSentenceChange exampleSentenceChange)
                {
#pragma warning disable CS0618
                    // Translation is obsolete and so populating it in new changes does not reflect reality
                    exampleSentenceChange.Translation = null;
#pragma warning restore CS0618
                }
            }, true),
            new SimpleOverride<MorphType>(context =>
            {
                if (context.Instance is MorphType morphType)
                {
                    // Only canonical morph types are supported, so align the generated Id (and Kind) with a canonical one.
                    if (!CanonicalMorphTypes.All.TryGetValue(morphType.Kind, out var canonical))
                    {
                        canonical = CanonicalMorphTypes.All.Values.OrderBy(_ => Random.Shared.Next()).First();
                        morphType.Kind = canonical.Kind;
                    }
                    morphType.Id = canonical.Id;
                }
            }, true),
            new SimpleOverride<CommentThread>(context =>
            {
                if (context.Instance is CommentThread thread && thread.Comments is { Count: > 0 })
                {
                    foreach (var comment in thread.Comments)
                        comment.CommentThreadId = thread.Id;
                }
            }, true)
        );
        return config;
    }
    protected static readonly AutoFaker Faker = new()
    {
        Config = GetAutoFakerConfig(),
    };

    protected static string GetJsonFilePath(string name, [CallerFilePath] string sourceFile = "")
    {
        return Path.Combine(
            Path.GetDirectoryName(sourceFile) ??
            throw new InvalidOperationException("Could not get directory of source file"),
            name);
    }

    private static readonly JsonSerializerOptions RegressionJsonOptions = new()
    {
        WriteIndented = true,
        Encoder = HarmonyJsonOptions.Encoder,
    };
    protected static string SerializeRegressionData(JsonArray jsonArray)
    {
        return JsonSerializer.Serialize(jsonArray, RegressionJsonOptions)
        // The "+" in DateTimeOffsets does not get escaped by our standard crdt serializer,
        // but it does here. Presumably, because it's reading it as a string and not a DateTimeOffset
        .Replace("\\u002B", "+");
    }

    private static readonly JsonWriterOptions GenericJsonWriterOptions = new()
    {
        Indented = true,
        Encoder = HarmonyJsonOptions.Encoder,
    };

    protected static string ToNormalizedIndentedJsonString(JsonNode element)
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

    protected static JsonArray ReadJsonArrayFromFile(string path)
    {
        if (!File.Exists(path)) return [];

        using var stream = File.OpenRead(path);
        var node = JsonNode.Parse(stream, null, new()
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip
        }) ?? throw new InvalidOperationException("Could not parse json array");
        return node.AsArray();
    }

    protected record LegacyRecord<T>(T Input, T Output);

    protected static Task VerifyRegressionDataUpToDate<T>(
        string fileNamePrefix,
        IEnumerable<Type> allTypes,
        Func<T, Type> typeOf,
        Func<Type, T> generate,
        [CallerFilePath] string sourceFile = "") where T : class
    {
        var legacyJsonArray = ReadJsonArrayFromFile(GetJsonFilePath($"{fileNamePrefix}.legacy.verified.txt", sourceFile));
        var latestJsonArray = ReadJsonArrayFromFile(GetJsonFilePath($"{fileNamePrefix}.latest.verified.txt", sourceFile));
        var newLatestJsonArray = new JsonArray();

        // step 1: refresh the output of legacy entries, so any that no longer round-trip to the same output get verified
        foreach (var legacyJsonNode in legacyJsonArray)
        {
            legacyJsonNode.Should().NotBeNull();
            var legacyJson = ToNormalizedIndentedJsonString(legacyJsonNode[nameof(LegacyRecord<T>.Input)]!);
            legacyJson.Should().NotBeNullOrWhiteSpace();
            var value = JsonSerializer.Deserialize<T>(legacyJson, HarmonyJsonOptions);
            value.Should().NotBeNull();
            var newLegacyOutputJson = JsonSerializer.Serialize(value, IndentedHarmonyJsonOptions);
            legacyJsonNode[nameof(LegacyRecord<T>.Output)] = JsonNode.Parse(newLegacyOutputJson);
            // the output is no longer round-trip tested in latest, so make sure it reserializes to itself
            var reserializedOutput = JsonSerializer.Deserialize<T>(newLegacyOutputJson, HarmonyJsonOptions);
            JsonSerializer.Serialize(reserializedOutput, IndentedHarmonyJsonOptions).Should().Be(newLegacyOutputJson);
        }

        // step 2: validate the round-tripping/output of latest entries, moving any that don't to legacy
        var stableTypes = new HashSet<Type>();
        foreach (var latestJsonNode in latestJsonArray)
        {
            latestJsonNode.Should().NotBeNull();
            var latestJson = ToNormalizedIndentedJsonString(latestJsonNode);
            latestJson.Should().NotBeNullOrWhiteSpace();
            var value = JsonSerializer.Deserialize<T>(latestJsonNode, HarmonyJsonOptions);
            value.Should().NotBeNull();
            var newLatestJson = JsonSerializer.Serialize(value, IndentedHarmonyJsonOptions);

            if (latestJson != newLatestJson)
            {
                // The current "latest" json doesn't match it's reserialized form.
                // I.e. it's no longer the latest. It's now legacy
                legacyJsonArray.Add(new JsonObject
                {
                    [nameof(LegacyRecord<T>.Input)] = latestJsonNode.DeepClone(),
                    [nameof(LegacyRecord<T>.Output)] = JsonNode.Parse(newLatestJson)
                });
            }
            else
            {
                // it's still the latest
                newLatestJsonArray.Add(latestJsonNode.DeepClone());
                stableTypes.Add(typeOf(value));
            }
        }

        // step 3: add a generated entry for any type no longer represented in latest.
        // If the new model only changes the representation of the same data then this might not be helpful.
        // However, we typically change the model in order to add new data, so the generated entry will exercise that new data.
        // Anyhow, it's much easier for a dev to remove unwanted entries than
        // to generate and insert them manually. We can remove this if it's too noisy.
        foreach (var type in allTypes.Where(type => !stableTypes.Contains(type)))
        {
            var serialized = JsonSerializer.Serialize(generate(type), IndentedHarmonyJsonOptions);
            newLatestJsonArray.Add(JsonNode.Parse(serialized));
        }

        return Task.WhenAll(
            Verify(SerializeRegressionData(legacyJsonArray), sourceFile: sourceFile)
                .UseStrictJson()
                .UseFileName($"{fileNamePrefix}.legacy"),
            Verify(SerializeRegressionData(newLatestJsonArray), sourceFile: sourceFile)
                .UseStrictJson()
                .UseFileName($"{fileNamePrefix}.latest")
        );
    }
}
