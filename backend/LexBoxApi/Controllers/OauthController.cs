using System.Security.Claims;
using System.Text.Json;
using LexBoxApi.Auth;
using LexCore.Auth;
using LexData;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace LexBoxApi.Controllers;

[ApiController]
[Route("/api/oauth")]
public class OauthController(
    IOpenIddictApplicationManager applicationManager,
    IOpenIddictAuthorizationManager authorizationManager,
    LexAuthService lexAuthService
) : ControllerBase
{

    [HttpGet("open-id-auth")]
    [HttpPost("open-id-auth")]
    [ProducesResponseType(400)]
    [ProducesDefaultResponseType]
    public async Task<ActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest();
        if (request is null)
        {
            return BadRequest();
        }

        if (IsAcceptRequest())
        {
            return await FinishSignIn(User, request);
        }

        // Retrieve the user principal stored in the authentication cookie.
        // If the user principal can't be extracted or the cookie is too old, redirect the user to the login page.
        var result = await HttpContext.AuthenticateAsync();
        if (!result.Succeeded ||
            result.Principal.Identity?.IsAuthenticated is not true ||
            request.HasPrompt(OpenIddictConstants.Prompts.Login) ||
            IsExpired(request, result))
        {
            // If the client application requested promptless authentication,
            // return an error indicating that the user is not logged in.
            if (request.HasPrompt(OpenIddictConstants.Prompts.None))
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] =
                            OpenIddictConstants.Errors.LoginRequired,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "The user is not logged in."
                    }));
            }

            // To avoid endless login -> authorization redirects, the prompt=login flag
            // is removed from the authorization request payload before redirecting the user.
            var prompt = string.Join(" ", request.GetPrompts().Remove(OpenIddictConstants.Prompts.Login));

            var parameters = Request.HasFormContentType
                ? Request.Form.Where(parameter => parameter.Key != OpenIddictConstants.Parameters.Prompt).ToList()
                : Request.Query.Where(parameter => parameter.Key != OpenIddictConstants.Parameters.Prompt).ToList();

            parameters.Add(KeyValuePair.Create(OpenIddictConstants.Parameters.Prompt, new StringValues(prompt)));

            return Challenge(
                authenticationSchemes: CookieAuthenticationDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties
                {
                    RedirectUri = Request.PathBase + Request.Path + QueryString.Create(parameters)
                });
        }

        var user = result.Principal;
        ArgumentNullException.ThrowIfNull(user);

        var requestClientId = request.ClientId;
        ArgumentException.ThrowIfNullOrEmpty(requestClientId);
        var application = await applicationManager.FindByClientIdAsync(requestClientId) ??
                          throw new InvalidOperationException(
                              "Details concerning the calling client application cannot be found.");
        var applicationId = await applicationManager.GetIdAsync(application) ??
                            throw new InvalidOperationException("The calling client application could not be found.");

        // Retrieve the permanent authorizations associated with the user and the calling client application.
        var authorizations = await authorizationManager.FindAsync(
            subject: GetUserId(user).ToString(),
            client: applicationId,
            status: OpenIddictConstants.Statuses.Valid,
            type: OpenIddictConstants.AuthorizationTypes.Permanent,
            scopes: request.GetScopes()).ToListAsync();

        switch (await applicationManager.GetConsentTypeAsync(application))
        {
            // If the consent is implicit or if an authorization was found,
            // return an authorization response without displaying the consent form.
            case OpenIddictConstants.ConsentTypes.Implicit:
            case OpenIddictConstants.ConsentTypes.External when authorizations.Count is not 0:
            case OpenIddictConstants.ConsentTypes.Explicit when authorizations.Count is not 0 && !request.HasPrompt(OpenIddictConstants.Prompts.Consent):

                return await FinishSignIn(user, request, applicationId, authorizations);

            // If the consent is external (e.g when authorizations are granted by a sysadmin),
            // immediately return an error if no authorization can be found in the database.
            case OpenIddictConstants.ConsentTypes.External when authorizations.Count is 0:
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.ConsentRequired,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "The logged in user is not allowed to access this client application."
                    }));

            // At this point, no authorization was found in the database and an error must be returned
            // if the client application specified prompt=none in the authorization request.
            case OpenIddictConstants.ConsentTypes.Explicit when request.HasPrompt(OpenIddictConstants.Prompts.None):
            case OpenIddictConstants.ConsentTypes.Systematic when request.HasPrompt(OpenIddictConstants.Prompts.None):
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.ConsentRequired,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "Interactive user consent is required."
                    }));

            // In every other case, send user to consent page
            default:
                var parameters = Request.HasFormContentType
                    ? Request.Form.ToList()
                    : Request.Query.ToList();
                var data = JsonSerializer.Serialize(parameters.ToDictionary(pair => pair.Key, pair => pair.Value.ToString()));
                var queryString = new QueryString()
                    .Add("appName", await applicationManager.GetDisplayNameAsync(application) ?? "Unknown app")
                    .Add("scope", request.Scope ?? "")
                    .Add("postback", data);
                return Redirect($"/authorize{queryString.Value}");
        }
    }

    private static bool IsExpired(OpenIddictRequest request, AuthenticateResult result)
    {
        // If a max_age parameter was provided, ensure that the cookie is not too old.
        return (request.MaxAge != null && result.Properties?.IssuedUtc != null &&
                DateTimeOffset.UtcNow - result.Properties.IssuedUtc >
                TimeSpan.FromSeconds(request.MaxAge.Value));
    }

    private bool IsAcceptRequest()
    {
        return Request.Method == "POST" && Request.Form.ContainsKey("submit.accept") && User.Identity?.IsAuthenticated == true;
    }

    [HttpPost("token")]
    [AllowAnonymous]
    public async Task<ActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
                      throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");
        // Retrieve the claims principal stored in the authorization code/refresh token.
        var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        if (!result.Succeeded || result.Principal.Identity?.IsAuthenticated is not true)
        {
            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                        "The token is no longer valid."
                }));
        }

        return await FinishSignIn(result.Principal, request);
    }

    private async Task<ActionResult> FinishSignIn(ClaimsPrincipal user, OpenIddictRequest request)
    {
        var requestClientId = request.ClientId;
        ArgumentException.ThrowIfNullOrEmpty(requestClientId);
        var application = await applicationManager.FindByClientIdAsync(requestClientId) ??
                          throw new InvalidOperationException(
                              "Details concerning the calling client application cannot be found.");
        // Retrieve the permanent authorizations associated with the user and the calling client application.
        var applicationId = await applicationManager.GetIdAsync(application) ?? throw new InvalidOperationException("The calling client application could not be found.");
        var authorizations = await authorizationManager.FindAsync(
            subject: GetUserId(user).ToString(),
            client: applicationId,
            status: OpenIddictConstants.Statuses.Valid,
            type: OpenIddictConstants.AuthorizationTypes.Permanent,
            scopes: request.GetScopes()).ToListAsync();

        //allow cors response for redirect hosts
        var redirectUrisAsync = await applicationManager.GetRedirectUrisAsync(application);
        Response.Headers.AccessControlAllowOrigin = redirectUrisAsync
            .Select(uri => new Uri(uri))
            .Where(uri => request.RedirectUri is not null && uri.Host == new Uri(request.RedirectUri).Host)
            .Select(uri => uri.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped)).ToArray();

        // Note: this check is here to ensure a malicious user can't abuse this POST-only endpoint and
        // force it to return a valid response without the external authorization.
        if (authorizations.Count is 0 &&
            await applicationManager.HasConsentTypeAsync(application, OpenIddictConstants.ConsentTypes.External))
        {
            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.ConsentRequired,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                        "The logged in user is not allowed to access this client application."
                }));
        }

        if (!await lexAuthService.CanUserLogin(GetUserId(user)))
        {
            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                        "User account is locked."
                }));
        }

        return await FinishSignIn(user, request, applicationId, authorizations);
    }
    private async Task<ActionResult> FinishSignIn(ClaimsPrincipal user, OpenIddictRequest request, string applicationId, List<object> authorizations)
    {
        var userId = GetUserId(user);
        // Create the claims-based identity that will be used by OpenIddict to generate tokens.
        var identity = new ClaimsIdentity(
            authenticationType: TokenValidationParameters.DefaultAuthenticationType,
            claims: user.Claims,
            nameType: OpenIddictConstants.Claims.Name,
            roleType: OpenIddictConstants.Claims.Role);

        // Note: in this sample, the granted scopes match the requested scope
        // but you may want to allow the user to uncheck specific scopes.
        // For that, simply restrict the list of scopes before calling SetScopes.
        // request scope may be null when exchanging tokens, so we want to keep the existing scopes
        if (!string.IsNullOrEmpty(request.Scope))
            identity.SetScopes(request.GetScopes());
        identity.SetAudiences(LexboxAudience.LexboxApi.ToString());
        // identity.SetResources(await _scopeManager.ListResourcesAsync(identity.GetScopes()).ToListAsync());

        // Automatically create a permanent authorization to avoid requiring explicit consent
        // for future authorization or token requests containing the same scopes.
        var authorization = authorizations.LastOrDefault();
        authorization ??= await authorizationManager.CreateAsync(
            identity: identity,
            subject : userId.ToString(),
            client  : applicationId,
            type    : OpenIddictConstants.AuthorizationTypes.Permanent,
            scopes  : identity.GetScopes());

        identity.SetAuthorizationId(await authorizationManager.GetIdAsync(authorization));
        identity.SetDestinations(GetDestinations);

        // Returning a SignInResult will ask OpenIddict to issue the appropriate access/identity tokens.
        return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private static Guid GetUserId(ClaimsPrincipal? principal)
    {
        var id = principal?.FindFirst(LexAuthConstants.IdClaimType)?.Value;
        if (id is null or [])
        {
            throw new InvalidOperationException("The user identifier cannot be found.");
        }
        return Guid.Parse(id);
    }

    private static IEnumerable<string> GetDestinations(Claim claim)
    {
        // Note: by default, claims are NOT automatically included in the access and identity tokens.
        // To allow OpenIddict to serialize them, you must attach them a destination, that specifies
        // whether they should be included in access tokens, in identity tokens or in both.

        var claimsIdentity = claim.Subject;
        ArgumentNullException.ThrowIfNull(claimsIdentity);
        switch (claim.Type)
        {
            case OpenIddictConstants.Claims.Name:
                yield return OpenIddictConstants.Destinations.AccessToken;

                if (claimsIdentity.HasScope(OpenIddictConstants.Scopes.Profile))
                    yield return OpenIddictConstants.Destinations.IdentityToken;

                yield break;
            case OpenIddictConstants.Claims.Username:
                yield return OpenIddictConstants.Destinations.AccessToken;

                if (claimsIdentity.HasScope(OpenIddictConstants.Scopes.Profile))
                    yield return OpenIddictConstants.Destinations.IdentityToken;

                yield break;

            case OpenIddictConstants.Claims.Email:
                yield return OpenIddictConstants.Destinations.AccessToken;

                if (claimsIdentity.HasScope(OpenIddictConstants.Scopes.Email))
                    yield return OpenIddictConstants.Destinations.IdentityToken;

                yield break;

            case OpenIddictConstants.Claims.Role:
                yield return OpenIddictConstants.Destinations.AccessToken;

                if (claimsIdentity.HasScope(OpenIddictConstants.Scopes.Roles))
                    yield return OpenIddictConstants.Destinations.IdentityToken;

                yield break;

            // Never include the security stamp in the access and identity tokens, as it's a secret value.
            case "AspNet.Identity.SecurityStamp": yield break;

            default:
                yield return OpenIddictConstants.Destinations.AccessToken;
                yield break;
        }
    }
}
