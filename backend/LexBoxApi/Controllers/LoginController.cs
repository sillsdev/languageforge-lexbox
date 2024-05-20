using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using LexBoxApi.Auth;
using LexBoxApi.Auth.Attributes;
using LexBoxApi.Models;
using LexBoxApi.Otel;
using LexBoxApi.Services;
using LexCore;
using LexCore.Auth;
using LexData;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using LexCore.Entities;
using System.Security.Claims;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace LexBoxApi.Controllers;

[ApiController]
[Route("/api/login")]
public class LoginController(
    LexAuthService lexAuthService,
    LexBoxDbContext lexBoxDbContext,
    LoggedInContext loggedInContext,
    EmailService emailService,
    UserService userService,
    TurnstileService turnstileService,
    ProjectService projectService,
    IOpenIddictApplicationManager applicationManager,
    IOpenIddictAuthorizationManager authorizationManager)
    : ControllerBase
{
    /// <summary>
    /// this endpoint is called when we can only pass a jwt in the query string. It redirects to the requested path
    /// and logs in using that jwt with a cookie
    /// </summary>
    [HttpGet("loginRedirect")]
    [AllowAnyAudience]
    public async Task<ActionResult> LoginRedirect(
        string jwt, // This is required because auth looks for a jwt in the query string
        string returnTo)
    {
        var user = loggedInContext.User;
        // A RegisterAccount token means there's no user account yet, so checking UpdatedDate makes no sense
        if (user.Audience != LexboxAudience.RegisterAccount)
        {
            var userUpdatedDate = await userService.GetUserUpdatedDate(user.Id);
            if (userUpdatedDate != user.UpdatedDate)
            {
                return await EmailLinkExpired();
            }
        }

        await HttpContext.SignInAsync(User,
            new AuthenticationProperties { IsPersistent = true });
        return Redirect(returnTo);
    }

    [HttpGet("google")]
    [AllowAnonymous]
    public IActionResult GoogleLogin(string? redirectTo = null)
    {
        if (string.IsNullOrEmpty(redirectTo)) redirectTo = "/home";
        var authProps = new AuthenticationProperties { RedirectUri = redirectTo };
        // If we want, we could look for expired-but-otherwise-valid JWT cookies and extract the user's email address, then:
        // authProps.SetParameter("login_hint", email);
        return Challenge(authProps, GoogleDefaults.AuthenticationScheme);
    }

    [NonAction]
    public async Task<string> CompleteGoogleLogin(ClaimsPrincipal? principal, string? returnTo)
    {
        returnTo ??= "/home";
        var googleId = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var foundGoogleId = false;
        var googleEmail = principal?.FindFirstValue(ClaimTypes.Email);
        var googleName = principal?.FindFirstValue(ClaimTypes.Name);
        var locale = principal?.FindFirstValue("locale");
        var (authUser, userEntity) = await lexAuthService.GetUserByGoogleId(googleId);
        if (authUser is not null)
        {
            foundGoogleId = true;
        }
        else
        {
            (authUser, userEntity) = await lexAuthService.GetUser(googleEmail);
        }

        if (authUser is null)
        {
            authUser = new LexAuthUser()
            {
                Id = Guid.NewGuid(),
                Audience = LexboxAudience.RegisterAccount,
                Name = googleName ?? "",
                Email = googleEmail,
                EmailVerificationRequired = null,
                Role = UserRole.user,
                UpdatedDate = DateTimeOffset.Now.ToUnixTimeSeconds(),
                CanCreateProjects = null,
                Locale = locale ?? LexCore.Entities.User.DefaultLocalizationCode,
                Locked = null,
            };
            var queryParams = new Dictionary<string, string?>()
            {
                { "email", googleEmail }, { "name", googleName }, { "returnTo", returnTo },
            };
            var queryString = QueryString.Create(queryParams);
            returnTo = "/register" + queryString.ToString();
        }

        if (userEntity is not null && !foundGoogleId)
        {
            userEntity.GoogleId = googleId;
            await lexBoxDbContext.SaveChangesAsync();
        }

        await HttpContext.SignInAsync(authUser.GetPrincipal("google"),
            new AuthenticationProperties { IsPersistent = true });
        return returnTo;
    }

    private async Task<ActionResult> EmailLinkExpired()
    {
        await HttpContext.SignOutAsync();
        return Redirect("/login?message=link_expired");
    }

    [HttpGet("verifyEmail")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LexAuthUser>> VerifyEmail(
        string jwt, // This is required because auth looks for a jwt in the query string
        string returnTo)
    {
        if (loggedInContext.User.EmailVerificationRequired == true)
        {
            return Unauthorized();
        }

        var userId = loggedInContext.User.Id;
        var user = await lexBoxDbContext.Users.FindAsync(userId);
        if (user == null) return NotFound();
        //users can verify their email even if the updated date is out of sync when not changing their email
        //this is to prevent some edge cases where changing their name and then using an old verify email link would fail
        if (user.Email != loggedInContext.User.Email &&
            user.UpdatedDate.ToUnixTimeSeconds() != loggedInContext.User.UpdatedDate)
        {
            return await EmailLinkExpired();
        }

        user.Email = loggedInContext.User.Email;
        user.EmailVerified = true;
        user.UpdateUpdatedDate();
        await lexBoxDbContext.SaveChangesAsync();
        await RefreshJwt();
        return Redirect(returnTo);
    }

    [HttpGet("open-id-auth")]
    [HttpPost("open-id-auth")]
    public async Task<ActionResult> Authorize(string? returnUrl = null)
    {
        var request = HttpContext.GetOpenIddictServerRequest();
        if (request is null)
        {
            return BadRequest();
        }

        // Retrieve the user principal stored in the authentication cookie.
        // If a max_age parameter was provided, ensure that the cookie is not too old.
        // If the user principal can't be extracted or the cookie is too old, redirect the user to the login page.
        var result = await HttpContext.AuthenticateAsync();
        if (!result.Succeeded || (request.MaxAge != null && result.Properties?.IssuedUtc != null &&
                                  DateTimeOffset.UtcNow - result.Properties.IssuedUtc >
                                  TimeSpan.FromSeconds(request.MaxAge.Value)))
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

            return Challenge(
                authenticationSchemes: CookieAuthenticationDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties
                {
                    RedirectUri = Request.PathBase + Request.Path + QueryString.Create(
                        Request.HasFormContentType ? [.. Request.Form] : [.. Request.Query])
                });
        }

        // If prompt=login was specified by the client application,
        // immediately return the user agent to the login page.
        if (request.HasPrompt(OpenIddictConstants.Prompts.Login))
        {
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

        //todo create identity from db
        // Create the claims-based identity that will be used by OpenIddict to generate tokens.
        var identity = new ClaimsIdentity(result.Principal!.Claims,
            authenticationType: TokenValidationParameters.DefaultAuthenticationType,
            nameType: OpenIddictConstants.Claims.Name,
            roleType: OpenIddictConstants.Claims.Role);
        identity.SetScopes(new[]
        {
            OpenIddictConstants.Scopes.OfflineAccess,
            OpenIddictConstants.Scopes.OpenId,
            OpenIddictConstants.Scopes.Email,
            OpenIddictConstants.Scopes.Profile
        }.Intersect(request.GetScopes()));
        identity.SetAudiences(LexboxAudience.LexboxApi.ToString());
        identity.SetDestinations(claim => claim.Type switch
        {
            // Note: always include acr and auth_time in the identity token as they must be flowed
            // from the authorization endpoint to the identity token returned from the token endpoint.
            OpenIddictConstants.Claims.AuthenticationContextReference or
                OpenIddictConstants.Claims.AuthenticationTime
                => ImmutableArray.Create(OpenIddictConstants.Destinations.IdentityToken),

            // Note: when an authorization code or access token is returned, don't add the profile, email,
            // phone and address claims to the identity tokens as they are returned from the userinfo endpoint.
                OpenIddictConstants.Claims.Subject or
                OpenIddictConstants.Claims.Name or
                OpenIddictConstants.Claims.Locale or
                OpenIddictConstants.Claims.UpdatedAt when
                identity.HasScope(OpenIddictConstants.Scopes.Profile)
                //todo consider if this should be enabled or not. It prevents the name from ending up in the ID token, but I guess that's supposed to be handled with the userinfo endpoint
                // && !request.HasResponseType(OpenIddictConstants.ResponseTypes.Code)
                // && !request.HasResponseType(OpenIddictConstants.ResponseTypes.Token)
                =>
                [
                    OpenIddictConstants.Destinations.AccessToken,
                    OpenIddictConstants.Destinations.IdentityToken
                ],

            OpenIddictConstants.Claims.Email when
                identity.HasScope(OpenIddictConstants.Scopes.Email) &&
                !request.HasResponseType(OpenIddictConstants.ResponseTypes.Code) &&
                !request.HasResponseType(OpenIddictConstants.ResponseTypes.Token) =>
                [
                    OpenIddictConstants.Destinations.AccessToken,
                    OpenIddictConstants.Destinations.IdentityToken
                ],

            _ => [OpenIddictConstants.Destinations.AccessToken]
        });

        return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpPost("token")]
    [AllowAnonymous]
    public async Task<ActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
                      throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");
        // Retrieve the claims principal stored in the authorization code/refresh token.
        var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        var lexAuthUser = result.Succeeded ? LexAuthUser.FromClaimsPrincipal(result.Principal) : null;
        if (!result.Succeeded || lexAuthUser is null)
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
        var requestClientId = request.ClientId;
        ArgumentException.ThrowIfNullOrEmpty(requestClientId);
        var application = await applicationManager.FindByClientIdAsync(requestClientId) ??
                          throw new InvalidOperationException("Details concerning the calling client application cannot be found.");
        var userId = lexAuthUser.Id.ToString();
        // Retrieve the permanent authorizations associated with the user and the calling client application.
        var authorizations = await authorizationManager.FindAsync(
            subject: userId,
            client : requestClientId,
            status : OpenIddictConstants.Statuses.Valid,
            type   : OpenIddictConstants.AuthorizationTypes.Permanent,
            scopes : request.GetScopes()).ToListAsync();

        //allow cors response for redirect hosts
        var redirectUrisAsync = await applicationManager.GetRedirectUrisAsync(application);
        Response.Headers.AccessControlAllowOrigin = redirectUrisAsync.Select(uri => new Uri(uri).GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped)).ToArray();

        // Note: this check is here to ensure a malicious user can't abuse this POST-only endpoint and
        // force it to return a valid response without the external authorization.
        if (authorizations.Count is 0 && await applicationManager.HasConsentTypeAsync(application, OpenIddictConstants.ConsentTypes.External))
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

        // Create the claims-based identity that will be used by OpenIddict to generate tokens.
        var identity = new ClaimsIdentity(
            authenticationType: TokenValidationParameters.DefaultAuthenticationType,
            nameType: OpenIddictConstants.Claims.Name,
            roleType: OpenIddictConstants.Claims.Role);

        // Add the claims that will be persisted in the tokens.
        identity.SetClaim(OpenIddictConstants.Claims.Subject, userId)
                .SetClaim(OpenIddictConstants.Claims.Email, lexAuthUser.Email)
                .SetClaim(OpenIddictConstants.Claims.Name, lexAuthUser.Name)
                .SetClaim(OpenIddictConstants.Claims.Role, lexAuthUser.Role.ToString());

        // Note: in this sample, the granted scopes match the requested scope
        // but you may want to allow the user to uncheck specific scopes.
        // For that, simply restrict the list of scopes before calling SetScopes.
        identity.SetScopes(request.GetScopes());
        identity.SetAudiences(LexboxAudience.LexboxApi.ToString());
        // identity.SetResources(await _scopeManager.ListResourcesAsync(identity.GetScopes()).ToListAsync());

        // Automatically create a permanent authorization to avoid requiring explicit consent
        // for future authorization or token requests containing the same scopes.
        var authorization = authorizations.LastOrDefault();
        authorization ??= await authorizationManager.CreateAsync(
            identity: identity,
            subject : userId,
            client  : requestClientId,
            type    : OpenIddictConstants.AuthorizationTypes.Permanent,
            scopes  : identity.GetScopes());

        identity.SetAuthorizationId(await authorizationManager.GetIdAsync(authorization));
        identity.SetDestinations(GetDestinations);

        // Returning a SignInResult will ask OpenIddict to issue the appropriate access/identity tokens.
        return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
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

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [AllowAnonymous]
    public async Task<ActionResult<LexAuthUser>> Login(LoginRequest loginRequest)
    {
        var (user, error) = await lexAuthService.Login(loginRequest);

        if (error is not null)
        {
            return Unauthorized(error.ToString());
        }
        else if (user is null)
        {
            return Unauthorized();
        }

        await userService.UpdateUserLastActive(user.Id);
        await userService.UpdatePasswordStrength(user.Id, loginRequest);
        await HttpContext.SignInAsync(user.GetPrincipal("Password"),
            new AuthenticationProperties { IsPersistent = true });
        return user;
    }

    [HttpPost("refresh")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LexAuthUser>> RefreshJwt()
    {
        var user = await lexAuthService.RefreshUser(loggedInContext.User.Id);
        if (user == null) return Unauthorized();
        return user;
    }

    [HttpPost("logout")]
    public async Task<ActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return Ok();
    }

    [HttpPost("forgotPassword")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(Dictionary<string, string[]>))]
    [ProducesDefaultResponseType]
    public async Task<ActionResult> ForgotPassword(ForgotPasswordInput input)
    {
        using var activity = LexBoxActivitySource.Get().StartActivity();
        var validToken = await turnstileService.IsTokenValid(input.TurnstileToken, input.Email);
        activity?.AddTag("app.turnstile_token_valid", validToken);
        if (!validToken)
        {
            ModelState.AddModelError<ForgotPasswordInput>(r => r.TurnstileToken, "token invalid");
            return ValidationProblem(ModelState);
        }

        await userService.ForgotPassword(input.Email);
        return Ok();
    }

    public record ResetPasswordRequest(
        [Required(AllowEmptyStrings = false)] string PasswordHash,
        int? PasswordStrength);

    [HttpPost("resetPassword")]
    [RequireAudience(LexboxAudience.ForgotPassword)]
    [RequireCurrentUserInfo]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesDefaultResponseType]
    public async Task<ActionResult> ResetPassword(ResetPasswordRequest request)
    {
        var passwordHash = request.PasswordHash;
        var lexAuthUser = loggedInContext.User;
        var user = await lexBoxDbContext.Users.FindAsync(lexAuthUser.Id);
        if (user == null) return NotFound();
        user.PasswordHash = PasswordHashing.HashPassword(passwordHash, user.Salt, true);
        user.PasswordStrength = UserService.ClampPasswordStrength(request.PasswordStrength);
        user.UpdateUpdatedDate();
        await lexBoxDbContext.SaveChangesAsync();
        await emailService.SendPasswordChangedEmail(user);
        //the old jwt is only valid for calling forgot password endpoints, we need to generate a new one
        if (lexAuthUser.Audience == LexboxAudience.ForgotPassword)
            await RefreshJwt();
        return Ok();
    }
}
