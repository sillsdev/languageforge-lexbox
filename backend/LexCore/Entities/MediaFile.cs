using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LexCore.Entities;

public class MediaFile : EntityBase
{
    // Id property comes from EntityBase

    /// <summary>
    /// path to the file, relative to the hg repo, or where the fwdata file is located
    /// </summary>
    public required string Filename { get; set; }
    public Guid ProjectId { get; set; }
    public FileMetadata? Metadata { get; set; } = new FileMetadata();

    [MemberNotNull(nameof(Metadata))]
    public void InitializeMetadataIfNeeded(string filePath)
    {
        if (Metadata is null)
        {
            var fileInfo = new FileInfo(filePath);
            int cappedSize = 0;
            if (fileInfo.Exists)
            {
                cappedSize = fileInfo.Length > int.MaxValue ? int.MaxValue : (int)fileInfo.Length;
            }
            Metadata = new FileMetadata
            {
                SizeInBytes = cappedSize,
            };
        }
    }

    public override string ToString()
    {
        return $"{nameof(Filename)}: {Filename}, {nameof(ProjectId)}: {ProjectId}, {nameof(Metadata)}: {Metadata}";
    }
}

public class FileMetadata
{
    public string? Sha256Hash { get; set; }
    public int? SizeInBytes { get; set; }
    public string? FileFormat { get; set; }
    public string? MimeType { get; set; }
    public string? Author { get; set; }
    public DateTimeOffset? UploadDate { get; set; }
    public MediaFileLicense? License { get; set; }

    // Other optional, not-yet-mapped properties like purpose, transcript, etc., should end up in here
    [JsonExtensionData]
    public IDictionary<string, object> ExtraFields { get; set; } = new Dictionary<string, object>();
    // NOTE: Any metadata that is a JSON object or array will NOT be merged, just replaced

    public void Merge(FileMetadata other)
    {
        if (other.SizeInBytes is not null && other.SizeInBytes.Value > 0) this.SizeInBytes = other.SizeInBytes;
        if (!string.IsNullOrEmpty(other.FileFormat)) this.FileFormat = other.FileFormat;
        if (!string.IsNullOrEmpty(other.MimeType)) this.MimeType = other.MimeType;
        if (!string.IsNullOrEmpty(other.Author)) this.Author = other.Author;
        if (other.UploadDate is not null) this.UploadDate = other.UploadDate;
        if (other.License is not null) this.License = other.License;
        foreach (var kv in other.ExtraFields)
        {
            ExtraFields[kv.Key] = kv.Value;
        }
    }
}

public static class FileMetadataProperties
{
    public static PropertyInfo[] properties { get; set; }
    public static string[] propertyNamesLC { get; set; }

    static FileMetadataProperties()
    {
        properties = typeof(FileMetadata).GetProperties();
        propertyNamesLC = properties.Select(p => p.Name.ToLowerInvariant()).ToArray();
    }

    public static bool IsMetadataProp(string prop)
    {
        return propertyNamesLC.Contains(prop.ToLowerInvariant());
    }
}

public class ApiMetadataEndpointResult() : FileMetadata
{
    public ApiMetadataEndpointResult(FileMetadata? other) : this()
    {
        if (other is not null) Merge(other);
    }

    public required string Filename { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MediaFileLicense
{
    CreativeCommons,
    CreativeCommonsShareAlike,
    Other,
}
