using System.ComponentModel.DataAnnotations;

namespace LexBoxApi.Auth;

public class JwtOptions
{
    public static JwtOptions TestingOptions => new()
        {
            Audience = "testing",
            Issuer = "unitTest",
            Lifetime = TimeSpan.FromMinutes(1),
            RefreshLifetime = TimeSpan.FromMinutes(1),
            Secret = "this is only a test but must be long",
            RefreshAudience = "testingRefresh"
        };

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
