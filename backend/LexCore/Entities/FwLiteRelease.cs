using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace LexCore.Entities;

public record FwLiteRelease(string Version, string Url);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FwLiteEdition
{
    Windows,
    Linux,
    Android,
    // ReSharper disable once InconsistentNaming
    iOS,
    Mac,
    //not supported for now, see note in FwLiteReleaseController.DownloadLatest
    WindowsAppInstaller
}

public record ShouldUpdateResponse(FwLiteRelease? Release)
{
    [MemberNotNullWhen(true, nameof(Release))]
    public bool Update => Release is not null;
}
