using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace WebApi.Auth;

public class RequiresLexBoxBasicAuth : IAuthorizationRequirement
{
}

public class BasicAuthHandler : AuthorizationHandler<RequiresLexBoxBasicAuth>
{
    private readonly ProxyAuthService _proxyAuthService;
    private readonly IHttpContextAccessor _contextAccessor;

    public BasicAuthHandler(ProxyAuthService proxyAuthService, IHttpContextAccessor contextAccessor)
    {
        _proxyAuthService = proxyAuthService;
        _contextAccessor = contextAccessor;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
        RequiresLexBoxBasicAuth requirement)
    {
        if (_contextAccessor.HttpContext is null)
        {
            context.Fail(new AuthorizationFailureReason(this, "not in a request context"));
            return;
        }

        var headersAuthorization =
            _contextAccessor.HttpContext.Request.Headers.Authorization.ToString()["Basic ".Length..];
        var basicAuthValue = Encoding.ASCII.GetString(Convert.FromBase64String(headersAuthorization))?.Split(":");
        var (username, password) = basicAuthValue switch
        {
            ["", ""] => (null, null),
            [var u, var p] => (u, p),
            _ => (null, null)
        };
        if (username is null || password is null)
        {
            context.Fail(new AuthorizationFailureReason(this, "Invalid request"));
            return;
        }

        if (await _proxyAuthService.IsAuthorized(username, password))
        {
            context.Succeed(requirement);
            return;
        }

        context.Fail(new AuthorizationFailureReason(this, "Invalid username or password"));
    }
}

public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public BasicAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(), Scheme.Name)));
    }
}