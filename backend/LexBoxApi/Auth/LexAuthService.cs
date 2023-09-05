using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LexBoxApi.Services;
using LexCore;
using LexCore.Auth;
using LexCore.Entities;
using LexData;
using LexData.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LexBoxApi.Auth;

public class LexAuthService
{
    public const string RefreshHeaderName = "lexbox-refresh-jwt";
    private readonly IOptions<JwtOptions> _userOptions;
    private readonly LexBoxDbContext _lexBoxDbContext;
    private readonly EmailService _emailService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LexAuthService(IOptions<JwtOptions> userOptions, LexBoxDbContext lexBoxDbContext, EmailService emailService, IHttpContextAccessor httpContextAccessor)
    {
        _userOptions = userOptions;
        _lexBoxDbContext = lexBoxDbContext;
        _emailService = emailService;
        _httpContextAccessor = httpContextAccessor;
    }

    public static TokenValidationParameters TokenValidationParameters(JwtOptions jwtOptions, bool forRefresh = false)
    {
        var validationParams = new TokenValidationParameters
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
        if (jwtOptions.ClockSkew is TimeSpan skew) validationParams.ClockSkew = skew;
        return validationParams;
    }

    private static SymmetricSecurityKey GetSigningKey(JwtOptions jwtOptions)
    {
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret));
    }

    public async Task ForgotPassword(string email)
    {
        var (lexAuthUser, user) = await GetUser(email);
        // we want to silently return if the user doesn't exist, so we don't leak information.
        if (lexAuthUser is null || user?.CanLogin() is not true) return;
        var (jwt, _) = GenerateJwt(lexAuthUser);
        await _emailService.SendForgotPasswordEmail(jwt, user);
    }

    public async Task<LexAuthUser?> Login(LoginRequest loginRequest)
    {
        var (lexAuthUser, user) = await GetUser(loginRequest.EmailOrUsername);
        if (user?.CanLogin() is not true) return null;

        var validPassword = PasswordHashing.IsValidPassword(loginRequest.Password,
            user.Salt,
            user.PasswordHash,
            loginRequest.PreHashedPassword);
        return validPassword ? lexAuthUser : null;
    }

    public async Task<LexAuthUser?> RefreshUser(Guid userId)
    {
        var dbUser = await _lexBoxDbContext.Users.Include(u => u.Projects).ThenInclude(p => p.Project)
            .FirstOrDefaultAsync(user => user.Id == userId);
        if (dbUser?.CanLogin() is not true) return null;
        var jwtUser = new LexAuthUser(dbUser);
        var context = _httpContextAccessor.HttpContext;
        ArgumentNullException.ThrowIfNull(context);
        await context.SignInAsync(jwtUser.GetPrincipal("Refresh"), new AuthenticationProperties { IsPersistent = true });
        context.Response.Headers.Add(RefreshHeaderName, "true");
        return jwtUser;
    }

    private async Task<(LexAuthUser? lexAuthUser, User? user)> GetUser(string emailOrUsername)
    {
        var user = await _lexBoxDbContext.Users
            .FilterByEmail(emailOrUsername)
            .Include(u => u.Projects).ThenInclude(p => p.Project)
            .FirstOrDefaultAsync();
        return (user == null ? null : new LexAuthUser(user), user);
    }

    public (string token, TimeSpan tokenLifetime) GenerateJwt(LexAuthUser user, TimeSpan? lifetime = null)
    {
        lifetime ??= TimeSpan.MaxValue;
        var options = _userOptions.Value;
        // use the min lifetime, prevents the caller setting a longer lifetime then is configured in the application.
        return GenerateToken(user, options.Audience, lifetime.Value > options.Lifetime ? options.Lifetime : lifetime.Value);
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
        JwtTicketDataFormat.FixUpProjectClaims(jwt);
        var token = handler.WriteToken(jwt);
        return (token, tokenLifetime);
    }
}
