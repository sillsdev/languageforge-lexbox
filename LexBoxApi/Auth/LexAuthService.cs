using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using LexCore;
using LexCore.Auth;
using LexData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LexBoxApi.Auth;

public class LexAuthService
{
    private readonly IOptions<JwtOptions> _userOptions;
    private readonly LexBoxDbContext _lexBoxDbContext;

    public LexAuthService(IOptions<JwtOptions> userOptions, LexBoxDbContext lexBoxDbContext)
    {
        _userOptions = userOptions;
        _lexBoxDbContext = lexBoxDbContext;
    }

    public static TokenValidationParameters TokenValidationParameters(JwtOptions jwtOptions, bool forRefresh = false)
    {
        return new TokenValidationParameters
        {
            RoleClaimType = LexAuthConstants.RoleClaimType,
            NameClaimType = LexAuthConstants.EmailClaimType,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = GetSigningKey(jwtOptions),
            ValidAudience = forRefresh ? jwtOptions.RefreshAudience : jwtOptions.Audience,
            ValidIssuer = jwtOptions.Issuer,

            RequireSignedTokens = true,
            RequireExpirationTime = true,

            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
        };
    }

    private static SymmetricSecurityKey GetSigningKey(JwtOptions jwtOptions)
    {
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret));
    }

    public async Task<LexAuthUser?> Login(string usernameOrEmail, string password)
    {
        var user = await _lexBoxDbContext.Users.Include(u => u.Projects).ThenInclude(p => p.Project)
            .FirstOrDefaultAsync(user => user.Email == usernameOrEmail || user.Username == usernameOrEmail);
        if (user == null) return null;

        var validPassword = PasswordHashing.RedminePasswordHash(password, user.Salt) == user.PasswordHash;
        return validPassword ? new LexAuthUser(user) : null;
    }

    public string GenerateJwt(LexAuthUser user)
    {
        var options = _userOptions.Value;
        return GenerateToken(user, options.Audience, options.Lifetime);
    }

    public string GenerateRefreshToken(LexAuthUser user)
    {
        return GenerateToken(user, _userOptions.Value.RefreshAudience, _userOptions.Value.RefreshLifetime);
    }

    private string GenerateToken(LexAuthUser user, string audience, TimeSpan tokenLifetime)
    {
        var jwtDate = DateTime.UtcNow;
        var options = _userOptions.Value;
        var jwt = new JwtSecurityToken(
            audience: audience,
            issuer: options.Issuer,
            claims: user.GetClaims(),
            notBefore: jwtDate,
            expires: jwtDate + tokenLifetime,
            signingCredentials: new SigningCredentials(
                GetSigningKey(options),
                SecurityAlgorithms.HmacSha256
            )
        );
        var token = new JwtSecurityTokenHandler().WriteToken(jwt);
        return token;
    }
}