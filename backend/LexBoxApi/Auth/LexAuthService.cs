using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LexBoxApi.Services;
using LexCore;
using LexCore.Auth;
using LexCore.Entities;
using LexData;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LexBoxApi.Auth;

public class LexAuthService
{
    private readonly IOptions<JwtOptions> _userOptions;
    private readonly LexBoxDbContext _lexBoxDbContext;
    private readonly EmailService _emailService;

    public LexAuthService(IOptions<JwtOptions> userOptions, LexBoxDbContext lexBoxDbContext, EmailService emailService)
    {
        _userOptions = userOptions;
        _lexBoxDbContext = lexBoxDbContext;
        _emailService = emailService;
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

    public async Task ForgotPassword(string email)
    {
        var (lexAuthUser, user) = await GetUser(email);
        // we want to silently return if the user doesn't exist, so we don't leak information.
        if (lexAuthUser is null || user is null) return;
        var (jwt, _) = GenerateJwt(lexAuthUser);
        await _emailService.SendForgotPasswordEmail(jwt, user);
    }

    public async Task<LexAuthUser?> Login(LoginRequest loginRequest)
    {
        var (lexAuthUser, user) = await GetUser(loginRequest.EmailOrUsername);
        if (user == null) return null;

        var validPassword = PasswordHashing.IsValidPassword(loginRequest.Password,
            user.Salt,
            user.PasswordHash,
            loginRequest.PreHashedPassword);
        return validPassword ? lexAuthUser : null;
    }

    public async Task<LexAuthUser?> RefreshUser(Guid userId)
    {
        var user = await _lexBoxDbContext.Users.Include(u => u.Projects).ThenInclude(p => p.Project)
            .FirstOrDefaultAsync(user => user.Id == userId);
        return user == null ? null : new LexAuthUser(user);
    }

    private async Task<(LexAuthUser? lexAuthUser, User? user)> GetUser(string emailOrUsername)
    {
        var user = await _lexBoxDbContext.Users.Include(u => u.Projects).ThenInclude(p => p.Project)
            .FirstOrDefaultAsync(user => user.Email == emailOrUsername || user.Username == emailOrUsername || user.PreviousEmail == emailOrUsername);
        return (user == null ? null : new LexAuthUser(user), user);
    }

    public (string token, TimeSpan tokenLifetime) GenerateJwt(LexAuthUser user)
    {
        var options = _userOptions.Value;
        return GenerateToken(user, options.Audience, options.Lifetime);
    }

    public (string token, TimeSpan tokenLifetime) GenerateRefreshToken(LexAuthUser user)
    {
        return GenerateToken(user, _userOptions.Value.RefreshAudience, _userOptions.Value.RefreshLifetime);
    }

    private (string token, TimeSpan tokenLifetime) GenerateToken(LexAuthUser user,
        string audience,
        TimeSpan tokenLifetime)
    {
        var jwtDate = DateTime.UtcNow;
        var options = _userOptions.Value;
        var identity = new ClaimsIdentity(JwtBearerDefaults.AuthenticationScheme);
        var id = Guid.NewGuid().ToString().GetHashCode().ToString("x", CultureInfo.InvariantCulture);
        identity.AddClaim(new Claim(JwtRegisteredClaimNames.Jti, id));
        identity.AddClaims(user.GetClaims());
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.CreateJwtSecurityToken(
            audience: audience,
            issuer: options.Issuer,
            subject: identity,
            notBefore: jwtDate,
            expires: jwtDate + tokenLifetime,
            signingCredentials: new SigningCredentials(
                GetSigningKey(options),
                SecurityAlgorithms.HmacSha256
            )
        );
        var token = handler.WriteToken(jwt);
        return (token, tokenLifetime);
    }
}
