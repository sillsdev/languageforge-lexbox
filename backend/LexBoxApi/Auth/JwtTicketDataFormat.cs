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
    private static readonly JwtSecurityTokenHandler _jwtSecurityTokenHandler = new();
    private const string _propsPrefix = "props";

    public JwtTicketDataFormat(IHttpContextAccessor httpContextAccessor, IOptions<JwtOptions> userOptions)
    {
        _httpContextAccessor = httpContextAccessor;
        _userOptions = userOptions;
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

    public static string ConvertAuthTicketToJwt(AuthenticationTicket data,
        string? purpose,
        JwtBearerOptions jwtBearerOptions,
        JwtOptions jwtUserOptions)
    {
        var jwtDate = DateTime.UtcNow;
        _jwtSecurityTokenHandler.MapInboundClaims = jwtBearerOptions.MapInboundClaims;
        var claimsIdentity = new ClaimsIdentity(data.Principal.Claims, data.Principal.Identity?.AuthenticationType);
        var securityTokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = jwtBearerOptions.TokenValidationParameters.ValidIssuer,
            Audience = Audience(purpose, jwtBearerOptions.TokenValidationParameters.ValidAudience),
            NotBefore = data.Properties.IssuedUtc?.UtcDateTime ?? jwtDate,
            Expires = data.Properties.ExpiresUtc?.UtcDateTime ?? jwtDate + jwtUserOptions.Lifetime,
            SigningCredentials = new SigningCredentials(jwtBearerOptions.TokenValidationParameters.IssuerSigningKey,
                SecurityAlgorithms.HmacSha256),
            Subject = claimsIdentity,
            Claims = data.Properties.Items.ToDictionary(kvp => _propsPrefix + kvp.Key, kvp => kvp.Value as object)
        };
        var token = _jwtSecurityTokenHandler.CreateJwtSecurityToken(securityTokenDescriptor);
        FixUpProjectClaims(token);
        return _jwtSecurityTokenHandler.WriteToken(token);
    }

    public static void FixUpProjectClaims(JwtSecurityToken token)
    {
        // if there's only 1 project it will be a stored in the payload as just an object and not an array.
        var proj = token.Payload[LexAuthConstants.ProjectsClaimType];
        if (proj is not IList<object>)
        {
            token.Payload[LexAuthConstants.ProjectsClaimType] = new List<object> { proj };
        }
    }

    private static string Audience(string? purpose, string validAudience)
    {
        if (string.IsNullOrEmpty(purpose)) return validAudience;
        return $"{validAudience}|{purpose}";
    }

    public AuthenticationTicket? Unprotect(string? protectedText)
    {
        return Unprotect(protectedText, null);
    }

    public AuthenticationTicket? Unprotect(string? protectedText, string? purpose)
    {
        var jwtBearerOptions = JwtBearerOptions ??
                               throw new ArgumentNullException(nameof(JwtBearerOptions), "options is null");
        var validationParameters = jwtBearerOptions.TokenValidationParameters.Clone();
        validationParameters.ValidAudience = Audience(purpose, validationParameters.ValidAudience);
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
                    principal.Claims.Where(c => c.Type.StartsWith(_propsPrefix))
                        .ToDictionary(c => c.Type[_propsPrefix.Length..], c => c.Value)!
                );
                foreach (var identity in principal.Identities)
                {
                    foreach (var claim in identity.Claims.ToArray())
                    {
                        if (claim.Type.StartsWith(_propsPrefix)) identity.TryRemoveClaim(claim);
                    }
                }
                return new AuthenticationTicket(principal, properties, CookieAuthenticationDefaults.AuthenticationScheme);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        return null;
    }
}
