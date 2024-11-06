namespace LcmCrdt;

public class CrdtProject(string name, string dbPath) : IProjectIdentifier
{
    public string Name { get; } = name;
    public string Origin { get; } = "CRDT";
    public string DbPath { get; } = dbPath;
    public ProjectData? Data { get; set; }
}

/// <summary>
///
/// </summary>
/// <param name="Name">Name of the project</param>
/// <param name="Id">Id, consistent across all clients, matches the project Id in Lexbox</param>
/// <param name="OriginDomain">Server to sync with, null if not synced</param>
/// <param name="ClientId">Unique id for this client machine</param>
/// <param name="FwProjectId">FieldWorks project id, aka LangProjectId</param>
public record ProjectData(string Name, Guid Id, string? OriginDomain, Guid ClientId, Guid? FwProjectId = null)
{
    public static string? GetOriginDomain(Uri? uri)
    {
        return uri?.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped);
    }
}
