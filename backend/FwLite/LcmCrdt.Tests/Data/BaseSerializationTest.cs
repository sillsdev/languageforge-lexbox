using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using MiniLcm.Tests.AutoFakerHelpers;
using Soenneker.Utils.AutoBogus;

namespace LcmCrdt.Tests.Data;

public abstract class BaseSerializationTest
{
    protected static readonly Lazy<JsonSerializerOptions> LazyOptions = new(() =>
    {
        var config = new CrdtConfig();
        LcmCrdtKernel.ConfigureCrdt(config);
        config.JsonSerializerOptions.ReadCommentHandling = JsonCommentHandling.Skip;
        return config.JsonSerializerOptions;
    });

    protected static readonly JsonSerializerOptions Options = LazyOptions.Value;
    protected static readonly JsonSerializerOptions OptionsIndented = new(Options)
    {
        WriteIndented = true,
    };
    protected static readonly AutoFaker Faker = new()
    {
        Config = AutoFakerDefault.MakeConfig(repeatCount: 1, minimalRichSpans: true)
    };

    protected static string GetJsonFilePath(string name, [CallerFilePath] string sourceFile = "")
    {
        return Path.Combine(
            Path.GetDirectoryName(sourceFile) ??
            throw new InvalidOperationException("Could not get directory of source file"),
            name);
    }

    protected static string SerializeRegressionData(JsonArray jsonArray)
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

    private static readonly JsonWriterOptions GenericJsonWriterOptions = new()
    {
        Indented = true,
        Encoder = Options.Encoder,
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
