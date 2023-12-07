using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using LexBoxApi.Auth;
using LexCore.Auth;
using LexCore.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Shouldly;

namespace Testing.LexCore;

public class LexAuthUserTests
{
    static LexAuthUserTests()
    {
        IdentityModelEventSource.ShowPII = true;
    }
    private readonly LexAuthService _lexAuthService = new LexAuthService(
        new OptionsWrapper<JwtOptions>(JwtOptions.TestingOptions),
        null!,
        null!,
        null!);

    private readonly LexAuthUser _user = new()
    {
        Id = Guid.NewGuid(),
        Email = "test@test.com",
        Role = UserRole.user,
        Name = "test",
        UpdatedDate = DateTimeOffset.Now.ToUnixTimeSeconds(),
        Projects = new[] { new AuthUserProject(ProjectRole.Manager, Guid.NewGuid()) }
    };

    private static readonly JwtBearerOptions JwtBearerOptions = new()
    {
        TokenValidationParameters = LexAuthService.TokenValidationParameters(JwtOptions.TestingOptions),
        MapInboundClaims = false
    };

    [Fact]
    public void CanGetClaimsFromUser()
    {
        var claims = _user.GetClaims().Select(c => c.ToString()).ToArray();
        var idClaim = new Claim(LexAuthConstants.IdClaimType, _user.Id.ToString());
        var emailClaim = new Claim(LexAuthConstants.EmailClaimType, _user.Email);
        var roleClaim = new Claim(LexAuthConstants.RoleClaimType, _user.Role.ToString());
        var projectClaim = new Claim("proj", _user.ProjectsJson);
        claims.ShouldSatisfyAllConditions(
            () => claims.ShouldContain(idClaim.ToString()),
            () => claims.ShouldContain(emailClaim.ToString()),
            () => claims.ShouldContain(roleClaim.ToString()),
            () => claims.ShouldContain(projectClaim.ToString())
        );
    }

    [Fact]
    public void CanRoundTripClaimsThroughAPrincipal()
    {
        var claims = _user.GetPrincipal("Testing");
        var newUser = LexAuthUser.FromClaimsPrincipal(claims);
        newUser.ShouldBeEquivalentTo(_user);
    }

    [Fact]
    public void CanRoundTripClaimsThroughJwt()
    {
        var originalJwt = new JwtSecurityToken(claims: _user.GetClaims());
        var tokenHandler = new JwtSecurityTokenHandler();
        var encodedJwt = tokenHandler.WriteToken(originalJwt);
        var outputJwt = tokenHandler.ReadJwtToken(encodedJwt);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(outputJwt.Claims, "Testing"));
        var newUser = LexAuthUser.FromClaimsPrincipal(principal);
        newUser.ShouldBeEquivalentTo(_user);
    }

    [Fact]
    public void CanRoundTripClaimsWhenUsingSecurityTokenDescriptor()
    {
        var jwtUserOptions = JwtOptions.TestingOptions;
        var jwt = JwtTicketDataFormat.ConvertAuthTicketToJwt(
            new AuthenticationTicket(_user.GetPrincipal("test"), "test"),
            "testing",
            JwtBearerOptions,
            jwtUserOptions
        );
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.ReadJwtToken(jwt);

        var json = Base64UrlEncoder.Decode(token.RawPayload);
        LexAuthUser? newUser;
        try
        {
            newUser = JsonSerializer.Deserialize<LexAuthUser>(json);
        }
        catch (JsonException e)
        {
            throw new JsonException("Could not deserialize user, json: " + json, e);
        }

        _user.ShouldBeEquivalentTo(newUser);
    }

    [Fact]
    public void CanRoundTripJwtFromUserThroughLexAuthService()
    {
        var (jwt, _) = _lexAuthService.GenerateJwt(_user);
        var tokenHandler = new JwtSecurityTokenHandler();
        var outputJwt = tokenHandler.ReadJwtToken(jwt);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(outputJwt.Claims, "Testing"));
        var newUser = LexAuthUser.FromClaimsPrincipal(principal);
        _user.ShouldBeEquivalentTo(newUser);
    }

    [Fact]
    public void CheckingJwtLength()
    {
        var user = _user with
        {
            Projects = Enumerable.Range(0, LexAuthUser.MaxProjectCount)
                .Select(i => new AuthUserProject(i % 2 == 0 ? ProjectRole.Manager : ProjectRole.Editor, Guid.NewGuid()))
                .ToArray()
        };
        var (jwt, _) = _lexAuthService.GenerateJwt(user);
        jwt.Length.ShouldBeLessThan(LexAuthUser.MaxJwtLength);
    }

    [Fact]
    public void CanRoundTripThroughRefresh()
    {
        var (forgotJwt, _) = _lexAuthService.GenerateJwt(_user, audience:LexboxAudience.ForgotPassword);
        //simulate parsing the token into a claims principal
        var tokenHandler = new JwtSecurityTokenHandler();
        var forgotPrincipal = new ClaimsPrincipal(new ClaimsIdentity(tokenHandler.ReadJwtToken(forgotJwt).Claims, "Testing"));

        //simulate redirect refreshing the token
        var redirectJwt = JwtTicketDataFormat.ConvertAuthTicketToJwt(
            new AuthenticationTicket(forgotPrincipal, "test"),
            "testing",
            JwtBearerOptions,
            JwtOptions.TestingOptions
        );

        var loggedInPrincipal = new ClaimsPrincipal(new ClaimsIdentity(tokenHandler.ReadJwtToken(redirectJwt).Claims, "Testing"));
        var newUser = LexAuthUser.FromClaimsPrincipal(loggedInPrincipal);
        (_user with { Audience = LexboxAudience.ForgotPassword }).ShouldBeEquivalentTo(newUser);
    }
}
