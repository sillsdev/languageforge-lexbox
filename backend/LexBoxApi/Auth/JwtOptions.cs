using System.ComponentModel.DataAnnotations;

namespace LexBoxApi.Auth;

public class JwtOptions
{
    public static JwtOptions TestingOptions => new()
        {
            Lifetime = TimeSpan.FromMinutes(1),
            RefreshLifetime = TimeSpan.FromMinutes(1),
            EmailJwtLifetime = TimeSpan.FromMinutes(1),
            Secret = "this is only a test but must be long",
            ClockSkew = TimeSpan.Zero
        };

    [Required]
    public required string Secret { get; init; }

    [Required]
    public required TimeSpan Lifetime { get; init; }
    [Required]
    public required TimeSpan EmailJwtLifetime { get; init; }

    [Required]
    public required TimeSpan RefreshLifetime { get; init; }
    public TimeSpan? ClockSkew { get; init; }
}
