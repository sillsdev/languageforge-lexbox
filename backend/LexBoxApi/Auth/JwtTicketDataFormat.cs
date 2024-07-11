using System.Collections;
using System.Collections.Frozen;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LexCore.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LexBoxApi.Auth;

public class JwtTicketDataFormat : ISecureDataFormat<AuthenticationTicket>
{
    private JwtBearerOptions? JwtBearerOptions =>
        _httpContextAccessor.HttpContext?.RequestServices.GetRequiredService<IOptionsSnapshot<JwtBearerOptions>>().Get(
            JwtBearerDefaults.AuthenticationScheme);

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IOptions<JwtOptions> _userOptions;
    private static readonly JwtSecurityTokenHandler JwtSecurityTokenHandler = new();
    private const string PropsPrefix = "props";
    private readonly ILogger<JwtTicketDataFormat> _logger;

    public JwtTicketDataFormat(IHttpContextAccessor httpContextAccessor,
        IOptions<JwtOptions> userOptions,
        ILogger<JwtTicketDataFormat> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _userOptions = userOptions;
        _logger = logger;
    }

    public string Protect(AuthenticationTicket data)
    {
        return Protect(data, null);
    }

    public string Protect(AuthenticationTicket data, string? purpose)
    {
        var jwtBearerOptions = JwtBearerOptions ??
                               throw new ArgumentNullException(nameof(JwtBearerOptions), "options is null");
        return ConvertAuthTicketToJwt(data, purpose, jwtBearerOptions, _userOptions.Value);
    }

    private static readonly FrozenSet<string> TicketPropertiesToExclude = FrozenSet.ToFrozenSet([".issued", ".expires"]);
    public static string ConvertAuthTicketToJwt(AuthenticationTicket data,
        string? purpose,
        JwtBearerOptions jwtBearerOptions,
        JwtOptions jwtUserOptions)
    {
        var jwtDate = DateTime.UtcNow;
        JwtSecurityTokenHandler.MapInboundClaims = jwtBearerOptions.MapInboundClaims;
        var claimsIdentity = new ClaimsIdentity(data.Principal.Claims.Where(c => c.Type != JwtRegisteredClaimNames.Jti), data.Principal.Identity?.AuthenticationType);
        var keyId = Guid.NewGuid().ToString().GetHashCode().ToString("x", CultureInfo.InvariantCulture);
        claimsIdentity.AddClaim(new Claim(JwtRegisteredClaimNames.Jti, keyId));
        //there may already be an audience claim, we want to reuse that if it exists, if not fallback to the default audience
        var audience = DetermineAudience(claimsIdentity) ?? jwtBearerOptions.TokenValidationParameters.ValidAudience;
        var securityTokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = jwtBearerOptions.TokenValidationParameters.ValidIssuer,
            Audience = audience,
            IssuedAt = data.Properties.IssuedUtc?.UtcDateTime ?? jwtDate,
            NotBefore = data.Properties.IssuedUtc?.UtcDateTime ?? jwtDate,
            Expires = data.Properties.ExpiresUtc?.UtcDateTime ?? (jwtDate + jwtUserOptions.Lifetime),
            SigningCredentials = new SigningCredentials(jwtBearerOptions.TokenValidationParameters.IssuerSigningKey,
                SecurityAlgorithms.HmacSha256),
            Subject = claimsIdentity,
            Claims = data.Properties.Items.Where(kvp => !TicketPropertiesToExclude.Contains(kvp.Key))
                .ToDictionary(kvp => PropsPrefix + kvp.Key, kvp => kvp.Value as object)
        };
        var token = JwtSecurityTokenHandler.CreateJwtSecurityToken(securityTokenDescriptor);
        FixUpArrayClaims(token);
        return JwtSecurityTokenHandler.WriteToken(token);
    }

    private static string? DetermineAudience(ClaimsIdentity identity)
    {
        var audienceClaim = identity.FindFirst(LexAuthConstants.AudienceClaimType);
        if (audienceClaim is null) return null;
        //we need to remove the audience claim because it'll get added to the token twice from the SecurityTokenDescriptor Audience property otherwise
        identity.TryRemoveClaim(audienceClaim);
        return audienceClaim.Value;
    }

    public static void FixUpArrayClaims(JwtSecurityToken token)
    {
        foreach (var claimName in LexAuthUser.LexAuthUserTypeInfo.Properties
                     .Where(p => p.PropertyType != typeof(string) && p.PropertyType.IsAssignableTo(typeof(IEnumerable)))
                     .Select(p => p.Name))
        {
            if (!token.Payload.TryGetValue(claimName, out var value))
            {
                continue; // no claim to fix up, so nothing to do
            }

            // if there's only 1 object it will be a stored in the payload as just an object and not an array.
            if (value is not IList<object>)
            {
                token.Payload[claimName] = new List<object> { value };
            }
        }
    }

    public AuthenticationTicket? Unprotect(string? protectedText)
    {
        return Unprotect(protectedText, (string?) null);
    }

    public AuthenticationTicket? Unprotect(string? protectedText, string? purpose)
    {
        var jwtBearerOptions = JwtBearerOptions ??
                               throw new ArgumentNullException(nameof(JwtBearerOptions), "options is null");
        return ConvertJwtToAuthTicket(protectedText, jwtBearerOptions, _logger);
    }

    public static AuthenticationTicket? ConvertJwtToAuthTicket(string? protectedText, JwtBearerOptions jwtBearerOptions, ILogger logger)
    {
        var validationParameters = jwtBearerOptions.TokenValidationParameters.Clone();
        foreach (var validator in jwtBearerOptions.SecurityTokenValidators)
        {
            try
            {
                if (!validator.CanReadToken(protectedText)) continue;
                var principal = validator.ValidateToken(protectedText,
                    validationParameters,
                    out var validatedToken);
                if (principal == null) continue;
                var properties = new AuthenticationProperties(
                    principal.Claims.Where(c => c.Type.StartsWith(PropsPrefix))
                        .ToDictionary(c => c.Type[PropsPrefix.Length..], c => c.Value)!
                );
                properties.IssuedUtc = validatedToken.ValidFrom;
                properties.ExpiresUtc = validatedToken.ValidTo;
                foreach (var identity in principal.Identities)
                {
                    foreach (var claim in identity.Claims.ToArray())
                    {
                        if (claim.Type.StartsWith(PropsPrefix)) identity.TryRemoveClaim(claim);
                    }
                }

                return new AuthenticationTicket(principal,
                    properties,
                    CookieAuthenticationDefaults.AuthenticationScheme);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error validating token");
            }
        }

        return null;
    }
}
