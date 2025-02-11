using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Humanizer;
using LexBoxApi.Auth;
using LexCore.Auth;
using LexCore.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using FluentAssertions;
using FluentAssertions.Execution;
using LexCore;

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
        null!);

    private readonly LexAuthUser _user = new()
    {
        Id = new Guid("f0db4c5e-9d4b-4121-9dc0-b7070713ae4a"),
        Email = "test@test.com",
        Role = UserRole.user,
        Name = "test",
        Username = "test",
        Locked = false,
        EmailVerificationRequired = false,
        CanCreateProjects = true,
        UpdatedDate = DateTimeOffset.Now.ToUnixTimeSeconds(),
        CreatedByAdmin = false,
        Locale = "en",
        Orgs = [ new AuthUserOrg(OrgRole.Admin, LexData.SeedingData.TestOrgId) ],
        Projects = new[]
        {
            new AuthUserProject(ProjectRole.Manager, new Guid("42f566c0-a4d2-48b5-a1e1-59c82289ff99"))
        },
        FeatureFlags = [FeatureFlag.FwLiteBeta]
    };

    private static readonly JwtBearerOptions JwtBearerOptions = new()
    {
        TokenValidationParameters = LexAuthService.TokenValidationParameters(JwtOptions.TestingOptions),
        MapInboundClaims = false
    };

    [Fact]
    public void EnsureAllClaimsAreRepresentedByTestUser()
    {
        var propsSet = _user.GetClaims().Select(c => c.Type);
        var propsWhichShouldBeUsed = LexAuthUser.LexAuthUserTypeInfo.Properties
            .Where(p => p.AttributeProvider?.IsDefined(typeof(JsonIgnoreAttribute), true) is false or null)
            .Select(p => p.Name);

        propsSet.Should().BeEquivalentTo(propsWhichShouldBeUsed);
    }

    [Fact]
    public void CanGetClaimsFromUser()
    {
        var claims = _user.GetClaims().Select(c => c.ToString()).ToArray();
        var idClaim = new Claim(LexAuthConstants.IdClaimType, _user.Id.ToString());
        ArgumentException.ThrowIfNullOrEmpty(_user.Email);
        var emailClaim = new Claim(LexAuthConstants.EmailClaimType, _user.Email);
        var roleClaim = new Claim(LexAuthConstants.RoleClaimType, _user.Role.ToString());
        var projectClaim = new Claim("proj", _user.ProjectsJson);
        var featureFlagClaim = new Claim(LexAuthConstants.FeatureFlagsClaimType, string.Join(",", _user.FeatureFlags));
        using (new AssertionScope())
        {
            claims.Should().Contain(idClaim.ToString());
            claims.Should().Contain(emailClaim.ToString());
            claims.Should().Contain(roleClaim.ToString());
            claims.Should().Contain(projectClaim.ToString());
            claims.Should().Contain(featureFlagClaim.ToString());
        }
    }

    [Fact]
    public void CanRoundTripClaimsThroughAPrincipal()
    {
        var claims = _user.GetPrincipal("Testing");
        var newUser = LexAuthUser.FromClaimsPrincipal(claims);
        newUser.Should().BeEquivalentTo(_user);
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
        newUser.Should().BeEquivalentTo(_user);
    }

    [Fact]
    public void CanRoundTripClaimsWhenUsingSecurityTokenDescriptor()
    {
        //truncate milliseconds because the jwt doesn't store them
        var expires =
            DateTimeOffset.FromUnixTimeSeconds((DateTimeOffset.UtcNow + TimeSpan.FromDays(1)).ToUnixTimeSeconds());
        var issuedAt = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        var jwtUserOptions = JwtOptions.TestingOptions;
        var jwt = JwtTicketDataFormat.ConvertAuthTicketToJwt(
            new AuthenticationTicket(_user.GetPrincipal("test"), "test")
            {
                Properties = { ExpiresUtc = expires, IssuedUtc = issuedAt }
            },
            "testing",
            JwtBearerOptions,
            jwtUserOptions
        );
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.ReadJwtToken(jwt);
        token.ValidTo.Should().Be(expires.DateTime);
        token.ValidFrom.Should().Be(issuedAt.DateTime);
        token.IssuedAt.Should().Be(issuedAt.DateTime);
        //props get converted to claims, but some we want to exclude because they are used elsewhere.
        token.Claims.Should().NotContain(c => c.Type == "props.issued" || c.Type == "props.expires");

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

        newUser.Should().BeEquivalentTo(_user);
    }

    [Fact]
    public void CanRoundTripFromAuthTicketToAuthTicket()
    {
        //truncate milliseconds because the jwt doesn't store them
        var expires = DateTimeOffset.FromUnixTimeSeconds((DateTimeOffset.UtcNow + TimeSpan.FromDays(1)).ToUnixTimeSeconds());
        var issuedAt = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        var jwtUserOptions = JwtOptions.TestingOptions;
        var ticket = new AuthenticationTicket(_user.GetPrincipal("test"), "test")
        {
            Properties =
            {
                Items = { { "test", "test" } },
                ExpiresUtc = expires,
                IssuedUtc = issuedAt,

            }
        };
        var jwt = JwtTicketDataFormat.ConvertAuthTicketToJwt(
            ticket,
            "testing",
            JwtBearerOptions,
            jwtUserOptions
        );
        var actualTicket = JwtTicketDataFormat.ConvertJwtToAuthTicket(jwt, JwtBearerOptions, NullLogger.Instance);
        actualTicket.Should().NotBeNull();
        actualTicket.Properties.IssuedUtc.Should().Be(ticket.Properties.IssuedUtc);
        actualTicket.Properties.ExpiresUtc.Should().Be(ticket.Properties.ExpiresUtc);
        //order by is because the order isn't important but the assertion fails if the order is different
        actualTicket.Properties.Items.OrderBy(kvp => kvp.Key)
            .Should().Equal(ticket.Properties.Items.OrderBy(kvp => kvp.Key));

        var newUser = LexAuthUser.FromClaimsPrincipal(actualTicket.Principal);
        newUser.Should().BeEquivalentTo(_user);
    }

    [Fact]
    public void CanRoundTripJwtFromUserThroughLexAuthService()
    {
        var (jwt, _, _) = _lexAuthService.GenerateJwt(_user);

        var tokenHandler = new JwtSecurityTokenHandler();
        var outputJwt = tokenHandler.ReadJwtToken(jwt);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(outputJwt.Claims, "Testing"));
        var newUser = LexAuthUser.FromClaimsPrincipal(principal);
        newUser.Should().BeEquivalentTo(_user);
    }

    private const string knownGoodJwt =
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJqdGkiOiIyYzEyNDA1NyIsInN1YiI6ImYwZGI0YzVlLTlkNGItNDEyMS05ZGMwLWI3MDcwNzEzYWU0YSIsImVtYWlsIjoidGVzdEB0ZXN0LmNvbSIsIm5hbWUiOiJ0ZXN0Iiwicm9sZSI6InVzZXIiLCJwcm9qIjoibTo0MmY1NjZjMGE0ZDI0OGI1YTFlMTU5YzgyMjg5ZmY5OSIsIm5iZiI6MTcwMjM3Mzk2OCwiZXhwIjoxNzAyMzc0MDI4LCJpYXQiOjE3MDIzNzM5NjksImlzcyI6IkxleGJveEFwaSIsImF1ZCI6IkxleGJveEFwaSJ9.YsAkP5oIX4nNkrSNSe-PNMR1pMaJassnNDJ3vmjMYQU";

    [Fact]
    public void CanParseFromKnownGoodJwt()
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var outputJwt = tokenHandler.ReadJwtToken(knownGoodJwt);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(outputJwt.Claims, "Testing"));
        var newUser = LexAuthUser.FromClaimsPrincipal(principal);
        newUser.Should().NotBeNull();
        newUser.UpdatedDate.Should().Be(0);
        //old jwt doesn't have updated date or orgs, we're ok with that so we correct the values to make the equivalence work
        newUser.Orgs = [ new AuthUserOrg(OrgRole.Admin, LexData.SeedingData.TestOrgId) ];
        newUser.UpdatedDate = _user.UpdatedDate;
        newUser.FeatureFlags = _user.FeatureFlags;
        // ditto for Username
        newUser.Username = _user.Username;
        // ditto for Locked, EmailVerificationRequired, CanCreateProjects, and CreatedByAdmin
        newUser.Should().BeEquivalentTo(_user,
            user => user.Excluding(u => u.Locked).Excluding(u => u.EmailVerificationRequired).Excluding(u => u.CanCreateProjects).Excluding(u => u.CreatedByAdmin));
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
        var (jwt, _, _) = _lexAuthService.GenerateJwt(user);
        jwt.Length.Should().BeLessThan(LexAuthUser.MaxJwtLength);
    }

    [Fact]
    public void CanRoundTripThroughRefresh()
    {
        var (forgotJwt, _, _) = _lexAuthService.GenerateJwt(_user with { Audience = LexboxAudience.ForgotPassword });
        //simulate parsing the token into a claims principal
        var tokenHandler = new JwtSecurityTokenHandler();
        var forgotPrincipal =
            new ClaimsPrincipal(new ClaimsIdentity(tokenHandler.ReadJwtToken(forgotJwt).Claims, "Testing"));

        //simulate redirect refreshing the token
        var redirectJwt = JwtTicketDataFormat.ConvertAuthTicketToJwt(
            new AuthenticationTicket(forgotPrincipal, "test"),
            "testing",
            JwtBearerOptions,
            JwtOptions.TestingOptions
        );

        var loggedInPrincipal =
            new ClaimsPrincipal(new ClaimsIdentity(tokenHandler.ReadJwtToken(redirectJwt).Claims, "Testing"));
        var newUser = LexAuthUser.FromClaimsPrincipal(loggedInPrincipal);
        newUser.Should().BeEquivalentTo(_user with { Audience = LexboxAudience.ForgotPassword });
    }
}
