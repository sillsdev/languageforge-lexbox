using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text;
using LexBoxApi.Otel;
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
    public const string JwtUpdatedHeader = "lexbox-jwt-updated";
    private readonly IOptions<JwtOptions> _userOptions;
    private readonly LexBoxDbContext _lexBoxDbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LexAuthService(IOptions<JwtOptions> userOptions,
        LexBoxDbContext lexBoxDbContext,
        IHttpContextAccessor httpContextAccessor)
    {
        _userOptions = userOptions;
        _lexBoxDbContext = lexBoxDbContext;
        _httpContextAccessor = httpContextAccessor;
    }

    public static TokenValidationParameters TokenValidationParameters(JwtOptions jwtOptions)
    {
        var validationParams = new TokenValidationParameters
        {
            RoleClaimType = LexAuthConstants.RoleClaimType,
            NameClaimType = LexAuthConstants.EmailClaimType,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = GetSigningKey(jwtOptions),
            //default audience, used for cookie tokens in JwtTicketDataFormat
            ValidAudience = LexboxAudience.LexboxApi.ToString(),
            ValidAudiences = Enum.GetNames<LexboxAudience>().Where(a => a != LexboxAudience.Unknown.ToString()),
            ValidIssuer = LexboxAudience.LexboxApi.ToString(),
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

    public async Task<bool> CanUserLogin(Guid id)
    {
        var user = await _lexBoxDbContext.Users.FindAsync(id);
        return user?.CanLogin() ?? false;
    }

    public async Task<(LexAuthUser?, LoginError?)> Login(LoginRequest loginRequest)
    {
        var (lexAuthUser, user) = await GetUser(loginRequest.EmailOrUsername);

        if (user is null) return (null, LoginError.BadCredentials);

        var validPassword = PasswordHashing.IsValidPassword(loginRequest.Password,
            user.Salt,
            user.PasswordHash,
            loginRequest.PreHashedPassword);

        if (!validPassword) return (null, LoginError.BadCredentials);

        if (user.CanLogin() is false) return (null, LoginError.Locked);

        return (lexAuthUser, null);
    }

    public async Task<LexAuthUser?> RefreshUser(Guid userId, string updatedValue = "all")
    {
        using var activity = LexBoxActivitySource.Get().StartActivity();
        var dbUser = await _lexBoxDbContext.Users
            .Include(u => u.Projects)
            .ThenInclude(p => p.Project)
            .Include(u => u.Organizations)
            .ThenInclude(o => o.Organization)
            .FirstOrDefaultAsync(user => user.Id == userId);
        if (dbUser is null)
        {
            activity?.AddTag("app.user.refresh", "user-not-found");
            return null;
        }
        if (!dbUser.CanLogin())
        {
            activity?.AddTag("app.user.refresh", "user-cannot-login");
            return null;
        }

        var jwtUser = new LexAuthUser(dbUser);
        var context = _httpContextAccessor.HttpContext;
        ArgumentNullException.ThrowIfNull(context);
        await context.SignInAsync(jwtUser.GetPrincipal("Refresh"),
            new AuthenticationProperties { IsPersistent = true });
        dbUser.LastActive = DateTimeOffset.UtcNow;
        await _lexBoxDbContext.SaveChangesAsync();
        context.Response.Headers[JwtUpdatedHeader] = updatedValue;
        activity?.AddTag("app.user.refresh", "success");
        return jwtUser;
    }

    public async Task<(LexAuthUser? lexAuthUser, User? user)> GetUser(string? emailOrUsername)
    {
        if (emailOrUsername is null) return (null, null);
        return await GetUser(UserEntityExtensions.FilterByEmailOrUsername(emailOrUsername));
    }

    public async Task<(LexAuthUser? lexAuthUser, User? user)> GetUserByGoogleId(string? googleId)
    {
        return await GetUser(u => u.GoogleId == googleId);
    }

    private async Task<(LexAuthUser? lexAuthUser, User? user)> GetUser(Expression<Func<User, bool>> predicate)
    {
        var user = await _lexBoxDbContext.Users
            .Where(predicate)
            .Include(u => u.Projects).ThenInclude(p => p.Project)
            .FirstOrDefaultAsync();
        return (user == null ? null : new LexAuthUser(user), user);
    }

    public (string token, DateTime expiresAt, TimeSpan lifetime) GenerateJwt(LexAuthUser user,
        bool useEmailLifetime = false)
    {
        var options = _userOptions.Value;
        var lifetime = (user.Audience, useEmailLifetime) switch
        {
            (_, true) => options.EmailJwtLifetime,
            (LexboxAudience.SendAndReceive, _) => options.SendReceiveJwtLifetime,
            (LexboxAudience.SendAndReceiveRefresh, _) => options.SendReceiveRefreshJwtLifetime,
            _ => options.Lifetime
        };
        return GenerateToken(user, user.Audience, lifetime);
    }

    private (string token, DateTime expiresAt, TimeSpan lifetime) GenerateToken(LexAuthUser user,
        LexboxAudience audience,
        TimeSpan tokenLifetime)
    {
        var jwtDate = DateTime.UtcNow;
        var options = _userOptions.Value;
        var identity = new ClaimsIdentity(JwtBearerDefaults.AuthenticationScheme);
        var id = Guid.NewGuid().ToString().GetHashCode().ToString("x", CultureInfo.InvariantCulture);
        identity.AddClaim(new Claim(JwtRegisteredClaimNames.Jti, id));
        identity.AddClaims(user.GetClaims().Where(c => c.Type != JwtRegisteredClaimNames.Aud));
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.CreateJwtSecurityToken(
            audience: audience.ToString(),
            issuer: LexboxAudience.LexboxApi.ToString(),
            subject: identity,
            notBefore: jwtDate,
            expires: jwtDate + tokenLifetime,
            signingCredentials: new SigningCredentials(
                GetSigningKey(options),
                SecurityAlgorithms.HmacSha256
            )
        );
        JwtTicketDataFormat.FixUpArrayClaims(jwt);
        var token = handler.WriteToken(jwt);

        return (token, jwt.ValidTo.ToUniversalTime(), tokenLifetime);
    }
}
