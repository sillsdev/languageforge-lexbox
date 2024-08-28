using System.ComponentModel.DataAnnotations;

namespace LocalWebApp.Auth;

public class AuthConfig
{
    [Required]
    public required Uri DefaultAuthority { get; set; }
    public required string ClientId { get; set; }
    public string CacheFileName { get; set; } = Path.GetFullPath("msal.cache");
    public bool SystemWebViewLogin { get; set; } = false;
}
