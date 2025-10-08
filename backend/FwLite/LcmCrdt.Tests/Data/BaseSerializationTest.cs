using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using LcmCrdt.Changes;
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
}
