using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using LexCore;
using LexCore.ServiceInterfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace LexSyncReverseProxy.Auth;
public class BasicAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string AuthScheme = "HgAuthScheme";
    private readonly IProxyAuthService _proxyAuthService;

    public BasicAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock,
        IProxyAuthService proxyAuthService) : base(options, logger, encoder, clock)
    {
        _proxyAuthService = proxyAuthService;
    }

    protected override async Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        //the Basic realm part is required by the HG client, otherwise it won't request again with a basic auth header
        Response.Headers.WWWAuthenticate = "Basic,Basic realm=\"SyncProxy\"";
        await base.HandleForbiddenAsync(properties);
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        //the Basic realm part is required by the HG client, otherwise it won't request again with a basic auth header
        Response.Headers.WWWAuthenticate = "Basic,Basic realm=\"SyncProxy\"";
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

        var principal = await _proxyAuthService.Login(new LoginRequest(password, username));
        if (principal is null)
            return AuthenticateResult.Fail("Invalid username or password");
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)
        );
    }
}