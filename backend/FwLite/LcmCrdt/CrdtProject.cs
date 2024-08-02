using System.Diagnostics.CodeAnalysis;
using MiniLcm;

namespace LcmCrdt;

public class CrdtProject(string name, string dbPath) : IProjectIdentifier
{
    public string Name { get; } = name;
    public string Origin { get; } = "CRDT";
    public string DbPath { get; } = dbPath;
}

public record ProjectData(string Name, Guid Id, string? OriginDomain, Guid ClientId)
{
    public static string? GetOriginDomain(Uri? uri)
    {
        return uri?.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped);
    }
}
