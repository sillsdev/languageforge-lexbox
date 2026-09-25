using System.Collections;
using System.Collections.Frozen;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using LexCore.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using JsonClaimValueTypes = Microsoft.IdentityModel.JsonWebTokens.JsonClaimValueTypes;

namespace LexBoxApi.Auth;

public class JwtTicketDataFormat : ISecureDataFormat<AuthenticationTicket>
{
    private JwtBearerOptions? JwtBearerOptions =>
        _httpContextAccessor.HttpContext?.RequestServices.GetRequiredService<IOptionsSnapshot<JwtBearerOptions>>().Get(
            JwtBearerDefaults.AuthenticationScheme);

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IOptions<JwtOptions> _userOptions;
    private static readonly JsonWebTokenHandler TokenHandler = new();
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
        var claimsIdentity = new ClaimsIdentity(data.Principal.Claims.Where(c => c.Type != JwtRegisteredClaimNames.Jti), data.Principal.Identity?.AuthenticationType);
        var keyId = Guid.NewGuid().ToString().GetHashCode().ToString("x", CultureInfo.InvariantCulture);
        claimsIdentity.AddClaim(new Claim(JwtRegisteredClaimNames.Jti, keyId));
        //there may already be an audience claim, we want to reuse that if it exists, if not fallback to the default audience
        var audience = DetermineAudience(claimsIdentity) ?? jwtBearerOptions.TokenValidationParameters.ValidAudience;
        var claims = data.Properties.Items.Where(kvp => !TicketPropertiesToExclude.Contains(kvp.Key))
            .ToDictionary(kvp => PropsPrefix + kvp.Key, kvp => kvp.Value as object);
        AddArrayClaims(claimsIdentity, claims);
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
            Claims = claims!
        };
        return TokenHandler.CreateToken(securityTokenDescriptor);
    }

    private static string? DetermineAudience(ClaimsIdentity identity)
    {
        var audienceClaim = identity.FindFirst(LexAuthConstants.AudienceClaimType);
        if (audienceClaim is null) return null;
        //we need to remove the audience claim because it'll get added to the token twice from the SecurityTokenDescriptor Audience property otherwise
        identity.TryRemoveClaim(audienceClaim);
        return audienceClaim.Value;
    }

    /// <summary>
    /// Adds the array properties of <see cref="LexAuthUser"/> to <paramref name="claims"/> as JSON arrays.
    /// When building a token from a <see cref="ClaimsIdentity"/> the handler only emits a JSON array when there
    /// are multiple claims with the same type; a single claim is written as a scalar. Entries in
    /// <see cref="SecurityTokenDescriptor.Claims"/> take precedence over subject claims, so this guarantees
    /// a one-element array still round-trips as an array.
    /// </summary>
    public static void AddArrayClaims(ClaimsIdentity identity, IDictionary<string, object?> claims)
    {
        foreach (var claimName in LexAuthUser.LexAuthUserTypeInfo.Properties
                     .Where(p => p.PropertyType != typeof(string) && p.PropertyType.IsAssignableTo(typeof(IEnumerable)))
                     .Select(p => p.Name))
        {
            var values = identity.FindAll(claimName).Select(ClaimValueToJsonValue).ToList();
            if (values.Count == 0) continue;
            claims[claimName] = values;
        }
    }

    // mirrors how Microsoft.IdentityModel converts a Claim into a JSON payload value
    private static object ClaimValueToJsonValue(Claim claim)
    {
        return claim.ValueType switch
        {
            JsonClaimValueTypes.Json or JsonClaimValueTypes.JsonArray => JsonSerializer.Deserialize<JsonElement>(claim.Value),
            ClaimValueTypes.Boolean => bool.Parse(claim.Value),
            ClaimValueTypes.Integer or ClaimValueTypes.Integer32 or ClaimValueTypes.Integer64 => long.Parse(claim.Value, CultureInfo.InvariantCulture),
            _ => claim.Value
        };
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
        if (string.IsNullOrEmpty(protectedText)) return null;
        var validationParameters = jwtBearerOptions.TokenValidationParameters.Clone();
        foreach (var handler in jwtBearerOptions.TokenHandlers)
        {
            try
            {
                // ISecureDataFormat.Unprotect is synchronous. Blocking here is safe: validation uses an in-memory
                // symmetric key with no metadata retrieval, so the task completes synchronously
                // (JsonWebTokenHandler.ValidateToken does the same internally).
                var result = handler.ValidateTokenAsync(protectedText, validationParameters).GetAwaiter().GetResult();
                if (!result.IsValid)
                {
                    logger.LogError(result.Exception, "Error validating token");
                    continue;
                }

                var principal = new ClaimsPrincipal(result.ClaimsIdentity);
                var validatedToken = result.SecurityToken;
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
