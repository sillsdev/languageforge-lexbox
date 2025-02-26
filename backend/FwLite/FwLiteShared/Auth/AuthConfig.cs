using System.ComponentModel.DataAnnotations;
using LcmCrdt;

namespace FwLiteShared.Auth;

public class AuthConfig
{
    public const string DefaultClientId = "becf2856-0690-434b-b192-a4032b72067f";
    [Required]
    public required LexboxServer[] LexboxServers { get; set; } = [];

    public required string ClientId { get; set; } = DefaultClientId;
    public string CacheFileName { get; set; } = Path.GetFullPath("msal.json");
    public bool SystemWebViewLogin { get; set; } = false;
    public object? ParentActivityOrWindow { get; set; }
    public Action? AfterLoginWebView { get; set; }

    public LexboxServer GetServerByAuthority(string authority)
    {
        return LexboxServers.FirstOrDefault(s => s.Authority.Authority == authority) ?? throw new ArgumentException($"Server {authority} not found");
    }

    public LexboxServer GetServer(ProjectData projectData)
    {
        var originDomain = projectData.OriginDomain;
        if (string.IsNullOrEmpty(originDomain)) throw new InvalidOperationException("No origin domain in project data");
        return GetServerByAuthority(new Uri(originDomain).Authority);
    }
    public LexboxServer GetServer(string serverName)
    {
        return LexboxServers.FirstOrDefault(s => s.DisplayName == serverName) ?? throw new ArgumentException($"Server {serverName} not found");
    }
}

public record LexboxServer(Uri Authority, string DisplayName)
{
    public string Id => Authority.Authority;
}
