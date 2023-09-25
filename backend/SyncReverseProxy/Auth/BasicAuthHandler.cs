using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using LexCore;
using LexCore.Auth;
using LexCore.ServiceInterfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace LexSyncReverseProxy.Auth;

public class BasicAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string AuthScheme = "HgAuthScheme";
    private readonly ILexProxyService _lexProxyService;
    private readonly IMemoryCache _memoryCache;

    public BasicAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock,
        ILexProxyService lexProxyService,
        IMemoryCache memoryCache) : base(options, logger, encoder, clock)
    {
        _lexProxyService = lexProxyService;
        _memoryCache = memoryCache;
    }

    protected override async Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        //the Basic realm part is required by the HG client, otherwise it won't request again with a basic auth header
        Response.Headers.WWWAuthenticate = "Basic realm=\"SyncProxy\"";
        await base.HandleForbiddenAsync(properties);
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        //the Basic realm part is required by the HG client, otherwise it won't request again with a basic auth header
        Response.Headers.WWWAuthenticate = "Basic realm=\"SyncProxy\"";
        await base.HandleChallengeAsync(properties);
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authHeader = Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(authHeader))
        {
            return AuthenticateResult.Fail("No authorization header");
        }

        var basicAuthValue = Encoding.ASCII.GetString(Convert.FromBase64String(authHeader["Basic ".Length..])).Split(":");
        var (username, password) = basicAuthValue switch
        {
            ["", ""] => (null, null),
            [var u, var p] => (u, p),
            _ => (null, null)
        };
        if (username is null || password is null)
        {
            return AuthenticateResult.Fail("Invalid Request");
        }

        var user = await GetUser(password, username);
        if (user is null)
            return AuthenticateResult.Fail("Invalid username or password");
        return AuthenticateResult.Success(new AuthenticationTicket(user.GetPrincipal(Scheme.Name), Scheme.Name));
    }

    private static readonly byte[] cacheKeySalt = RandomNumberGenerator.GetBytes(128 / 8);

    private async ValueTask<LexAuthUser?> GetUser(string password, string username)
    {
        //todo utilize LfMerge trust token to let in LfMerge as an admin #291
        var cacheKey = Convert.ToBase64String(KeyDerivation.Pbkdf2(
            username + '|' + password,
            cacheKeySalt,
            KeyDerivationPrf.HMACSHA256,
            1000000,
            256 / 8));
        var key = "GetUserBasicAuth|" + cacheKey;
        if (_memoryCache.TryGetValue(key, out LexAuthUser? user)) return user;

        using var entry = _memoryCache.CreateEntry(key);
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
        entry.SlidingExpiration = TimeSpan.FromSeconds(15);

        user = await _lexProxyService.Login(new LoginRequest(password, username));
        entry.Value = user;
        return user;
    }
}
