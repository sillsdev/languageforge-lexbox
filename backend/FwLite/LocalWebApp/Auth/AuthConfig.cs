using System.ComponentModel.DataAnnotations;

namespace LocalWebApp.Auth;

public class AuthConfig
{
    [Required]
    public required LexboxServer[] LexboxServers { get; set; }
    public required string ClientId { get; set; }
    public string CacheFileName { get; set; } = Path.GetFullPath("msal.json");
    public bool SystemWebViewLogin { get; set; } = false;
    public LexboxServer DefaultServer => LexboxServers.First();

    public LexboxServer GetServerByAuthority(string authority)
    {
        return LexboxServers.FirstOrDefault(s => s.Authority.Authority == authority) ?? throw new ArgumentException($"Server {authority} not found");
    }
    public LexboxServer GetServer(string serverName)
    {
        return LexboxServers.FirstOrDefault(s => s.DisplayName == serverName) ?? throw new ArgumentException($"Server {serverName} not found");
    }
}

public record LexboxServer(Uri Authority, string DisplayName);
