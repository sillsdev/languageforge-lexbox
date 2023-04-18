using System.ComponentModel.DataAnnotations;

namespace LexBoxApi.Auth;

public class JwtOptions
{
    [Required]
    public required string Secret { get; init; }

    [Required]
    public required string Audience { get; init; }

    [Required]
    public required string RefreshAudience { get; init; }

    [Required]
    public required string Issuer { get; init; }

    [Required]
    public required TimeSpan Lifetime { get; init; }

    [Required]
    public required TimeSpan RefreshLifetime { get; init; }
}