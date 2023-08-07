using System.ComponentModel.DataAnnotations;

namespace LexBoxApi.Config;

public class CloudFlareConfig
{
    [Required]
    public required string TurnstileKey { get; init; }
    public string? AllowDomain { get; init; }
}
