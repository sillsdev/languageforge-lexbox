using System.Text.Json;
using System.Text.Json.Serialization;

namespace MiniLcm.Media;

[JsonConverter(typeof(MediaUriJsonConverter))]
public readonly record struct MediaUri
{
    public static readonly MediaUri NotFound = new MediaUri(Guid.Empty, "not-found");
    public static readonly string NotFoundString = NotFound.ToString();
    public const string Scheme = "sil-media";
    public MediaUri(Guid fileId, string authority)
    {
        FileId = fileId;
        Authority = authority;
    }

    public MediaUri(string uri) : this(new Uri(uri))
    {

    }

    public MediaUri(Uri uri)
    {
        if (uri.Scheme != Scheme) throw new ArgumentException($"Invalid scheme, scheme must be {Scheme}", nameof(uri));
        if (uri.Segments.Length < 2) throw new ArgumentException("Invalid URI, must contain a path which is the FileId", nameof(uri));
        FileId = Guid.Parse(uri.Segments[^1]);
        Authority = uri.Authority;
    }

    public Uri ToUri()
    {
        return new Uri($"{Scheme}://{Authority}/{FileId}");
    }

    public override string ToString()
    {
        return $"{Scheme}://{Authority}/{FileId}";
    }

    public Guid FileId { get; init; }
    public string Authority { get; init; }
}

public class MediaUriJsonConverter : JsonConverter<MediaUri>
{
    public override MediaUri Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var uri = reader.GetString();
        if (uri is null) return MediaUri.NotFound;
        return new MediaUri(uri);
    }

    public override void Write(Utf8JsonWriter writer, MediaUri value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
