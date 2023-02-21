namespace LexBoxApi.Auth;

public class JwtOptions
{
    public required string Secret { get; init; }
    public required string Audience { get; init; }
    public required string RefreshAudience { get; init; }
    public required string Issuer { get; init; }
    public required TimeSpan Lifetime { get; init; }
    public required TimeSpan RefreshLifetime { get; init; }
}