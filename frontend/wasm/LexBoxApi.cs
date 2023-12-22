using System;
using System.Runtime.InteropServices.JavaScript;
using lexboxClientContracts;
using System.Threading.Tasks;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Text.Json.Serialization;

// Console.WriteLine("Hello, Browser!!")

public class WritingSystemIdJsonConverter : JsonConverter<WritingSystemId>
{
    public override WritingSystemId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return WritingSystemId.Parse(reader.GetString(), null);
    }

    public override void Write(Utf8JsonWriter writer, WritingSystemId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }

    public override WritingSystemId ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => WritingSystemId.Parse(reader.GetString(), null);

    public override void WriteAsPropertyName(Utf8JsonWriter writer, WritingSystemId value, JsonSerializerOptions options)
    {
        writer.WritePropertyName(value.ToString());
    }
}

public partial class LexBoxApi
{
    private static readonly ILexboxApi _api = new InMemoryApi();

    [JSExport]
    internal static async Task<string> GetEntries()
    {
        var entries = await _api.GetEntries();
        return JsonSerializer.Serialize(entries, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new WritingSystemIdJsonConverter() }
        });
    }

    [JSExport]
    internal static async Task SetLexeme(string id, string value)
    {
        var guid = Guid.Parse(id);
        await _api.UpdateEntry(guid, new JsonPatchUpdateBuilder<IEntry>()
            .Set(e => e.LexemeForm.Values["en"], value)
            .Build());
    }

    [JSImport("window.location.href", "main.js")]
    internal static partial string GetHRef();
}
