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
using Xunit.Abstractions;

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
        Id = new Guid("f0db4c5e-9d4b-4121-9dc0-b7070713ae4a"),
        Email = "test@test.com",
        Role = UserRole.user,
        Name = "test",
        Username = "test",
        Locked = false,
        EmailVerificationRequired = false,
        CanCreateProjects = true,
        UpdatedDate = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds(),
        CreatedByAdmin = false,
        Locale = "en",
        Orgs = [ new AuthUserOrg(OrgRole.Admin, LexData.SeedingData.TestOrgId) ],
        Projects = new[]
        {
            new AuthUserProject(ProjectRole.Manager, new Guid("42f566c0-a4d2-48b5-a1e1-59c82289ff99"))
        },
        FeatureFlags = [FeatureFlag.FwLiteBeta],
        Audience = LexboxAudience.LexboxApi,
        Scopes = [LexboxAuthScope.LexboxApi, LexboxAuthScope.email]
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
        var jwt = _lexAuthService.GenerateJwt(_user, TimeSpan.Zero);

        var tokenHandler = new JwtSecurityTokenHandler();
        var outputJwt = tokenHandler.ReadJwtToken(jwt);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(outputJwt.Claims, "Testing"));
        var newUser = LexAuthUser.FromClaimsPrincipal(principal);
        newUser.Should().BeEquivalentTo(_user);
    }

    private const string knownGoodJwt =
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJqdGkiOiIyYzEyNDA1NyIsInN1YiI6ImYwZGI0YzVlLTlkNGItNDEyMS05ZGMwLWI3MDcwNzEzYWU0YSIsImVtYWlsIjoidGVzdEB0ZXN0LmNvbSIsIm5hbWUiOiJ0ZXN0Iiwicm9sZSI6InVzZXIiLCJwcm9qIjoibTo0MmY1NjZjMGE0ZDI0OGI1YTFlMTU5YzgyMjg5ZmY5OSIsIm5iZiI6MTcwMjM3Mzk2OCwiZXhwIjoxNzAyMzc0MDI4LCJpYXQiOjE3MDIzNzM5NjksImlzcyI6IkxleGJveEFwaSIsImF1ZCI6IkxleGJveEFwaSJ9.YsAkP5oIX4nNkrSNSe-PNMR1pMaJassnNDJ3vmjMYQU";

    private const string knownGoodJwt2 =
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJqdGkiOiI3MzBmMjEzOSIsInN1YiI6ImYwZGI0YzVlLTlkNGItNDEyMS05ZGMwLWI3MDcwNzEzYWU0YSIsImRhdGUiOjE2NzI1MzEyMDAsImVtYWlsIjoidGVzdEB0ZXN0LmNvbSIsInVzZXIiOiJ0ZXN0IiwibmFtZSI6InRlc3QiLCJyb2xlIjoidXNlciIsIm9yZ3MiOlt7IlJvbGUiOiJBZG1pbiIsIk9yZ0lkIjoiMjkyYzgwZTYtYTgxNS00Y2QxLTllYTItMzRiZDAxMjc0ZGU2In1dLCJmZWF0IjpbIkZ3TGl0ZUJldGEiXSwicHJvaiI6Im06NDJmNTY2YzBhNGQyNDhiNWExZTE1OWM4MjI4OWZmOTkiLCJsb2NrIjpmYWxzZSwidW52ZXIiOmZhbHNlLCJta3Byb2oiOnRydWUsImNyZWF0IjpmYWxzZSwibG9jIjoiZW4iLCJuYmYiOjE3NDA0NTMyMTYsImV4cCI6MTc0MDQ1MzI3NiwiaWF0IjoxNzQwNDUzMjE2LCJpc3MiOiJMZXhib3hBcGkiLCJhdWQiOiJMZXhib3hBcGkifQ.6L_tw1Q9OUIoxiKUxQHJdA2t_Abm8t84rg0fQIdQB40";
    [Theory]
    [InlineData(knownGoodJwt, 1)]//version is arbitrary, just used to determine how to test
    [InlineData(knownGoodJwt2, 2)]
    public void CanParseFromKnownGoodJwt(string jwt, int version)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var outputJwt = tokenHandler.ReadJwtToken(jwt);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(outputJwt.Claims, "Testing"));
        var newUser = LexAuthUser.FromClaimsPrincipal(principal);
        newUser.Should().NotBeNull();
        using var _ = new AssertionScope();
        newUser.Id.Should().Be(_user.Id);
        newUser.Email.Should().Be(_user.Email);
        newUser.Name.Should().Be(_user.Name);
        newUser.Role.Should().Be(_user.Role);
        newUser.Projects.Should().BeEquivalentTo(_user.Projects);
        newUser.Audience.Should().Be(_user.Audience);
        newUser.HasScope(LexboxAuthScope.LexboxApi).Should().BeTrue();

        if (version == 1)
        {
            newUser.UpdatedDate.Should().Be(0);
            return;
        }

        newUser.UpdatedDate.Should().Be(_user.UpdatedDate);
        newUser.Username.Should().Be(_user.Username);
        newUser.Orgs.Should().BeEquivalentTo(_user.Orgs);
        newUser.FeatureFlags.Should().BeEquivalentTo(_user.FeatureFlags);
        newUser.Locked.Should().Be(_user.Locked);
        newUser.EmailVerificationRequired.Should().Be(_user.EmailVerificationRequired);
        newUser.CanCreateProjects.Should().Be(_user.CanCreateProjects);
        newUser.CreatedByAdmin.Should().Be(_user.CreatedByAdmin);
        newUser.Locale.Should().Be(_user.Locale);
        newUser.IsAdmin.Should().Be(_user.IsAdmin);
        if (version == 2)
        {
            //nothing yet as it matches prod
        }
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
        var jwt = _lexAuthService.GenerateJwt(user, TimeSpan.Zero);
        jwt.Length.Should().BeLessThan(LexAuthUser.MaxJwtLength);
    }

    [Fact]
    public void CanRoundTripThroughRefresh()
    {
        var (forgotJwt, _) = _lexAuthService.GenerateEmailJwt(_user with { Audience = LexboxAudience.ForgotPassword });
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

    [Fact]
    public void HasScopeShouldOnlyFallbackToAudienceIfThereIsNoScope()
    {
        var user = _user with { ScopeString = null, Audience = LexboxAudience.LexboxApi };
        user.HasScope(LexboxAuthScope.LexboxApi).Should().BeTrue();
        user.HasScope(LexboxAuthScope.RegisterAccount).Should().BeFalse();

        user = user with { Scopes = [LexboxAuthScope.openid], Audience = LexboxAudience.LexboxApi };
        user.HasScope(LexboxAuthScope.LexboxApi).Should().BeFalse("there is a scope so we don't fallback to the audience");
        user.HasScope(LexboxAuthScope.openid).Should().BeTrue();
    }
}
