using System.Text.Json.Serialization;
using LcmCrdt.Project;
using Microsoft.Extensions.Caching.Memory;

namespace LcmCrdt;

public class CrdtProject(string code, string dbPath) : IProjectIdentifier
{
    public CrdtProject(string code, string dbPath, ProjectDataCache projectDataCache) : this(code, dbPath)
    {
        Data = projectDataCache.CachedProjectData(this);
    }

    /// <summary>
    /// Actually the Lexbox project code, not the name
    /// </summary>
    public string Name { get; } = code;
    public ProjectDataFormat DataFormat => ProjectDataFormat.Harmony;
    public string DbPath { get; } = dbPath;
    public ProjectData? Data { get; set; }
}

/// <summary>
///
/// </summary>
/// <param name="Name">Display name of the project</param>
/// <param name="Code">Unique code of the project</param>
/// <param name="Id">Id, consistent across all clients, matches the project Id in Lexbox</param>
/// <param name="OriginDomain">Server to sync with, null if not synced</param>
/// <param name="ClientId">Unique id for this client machine</param>
/// <param name="FwProjectId">FieldWorks project id, aka LangProjectId</param>
public record ProjectData(string Name, string Code, Guid Id, string? OriginDomain, Guid ClientId, Guid? FwProjectId = null, string? LastUserName = null, string? LastUserId = null,
    UserProjectRole Role = UserProjectRole.Unknown)
{
    public static string? GetOriginDomain(Uri? uri)
    {
        return uri?.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped);
    }

    public string? ServerId => Uri.TryCreate(OriginDomain, UriKind.Absolute, out var uri) ? uri.Authority : null;
    public bool IsReadonly => Role is not UserProjectRole.Editor and not UserProjectRole.Manager;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserProjectRole
{
    Unknown,
    Manager,
    Editor,
    Observer
}
