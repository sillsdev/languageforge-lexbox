using System.Diagnostics.CodeAnalysis;

namespace LexCore.Entities;

public record FwLiteRelease(string Version, string Url);

public record ShouldUpdateResponse(FwLiteRelease? Release)
{
    [MemberNotNullWhen(true, nameof(Release))]
    public bool Update => Release is not null;
}
