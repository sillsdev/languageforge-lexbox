using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using LexBoxApi.Auth;
using LexCore.Auth;
using LexCore.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Shouldly;

namespace Testing.LexCore;

public class LexAuthUserTests
{
    private readonly LexAuthUser _user = new()
    {
        Id = Guid.NewGuid(),
        Email = "test@test.com",
        Role = UserRole.user,
        Name = "test",
        Projects = new[]
        {
            new AuthUserProject("test-flex", ProjectRole.Manager, Guid.NewGuid())
        }
    };

    [Fact]
    public void CanGetClaimsFromUser()
    {
        var claims = _user.GetClaims().Select(c => c.ToString()).ToArray();
        var idClaim = new Claim(LexAuthConstants.IdClaimType, _user.Id.ToString());
        var emailClaim = new Claim(LexAuthConstants.EmailClaimType, _user.Email);
        var roleClaim = new Claim(LexAuthConstants.RoleClaimType, _user.Role.ToString());
        var projectClaim = new Claim("proj", JsonSerializer.Serialize(_user.Projects[0]));
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
        _user.ShouldBeEquivalentTo(newUser);
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
        _user.ShouldBeEquivalentTo(newUser);
    }

    [Fact]
    public void CanRoundTripClaimsWhenUsingSecurityTokenDescriptor()
    {
        var jwtUserOptions = JwtOptions.TestingOptions;
        var jwt = JwtTicketDataFormat.ConvertAuthTicketToJwt(
            new AuthenticationTicket(_user.GetPrincipal("test"), "test"),
            "testing",
            new JwtBearerOptions
            {
                TokenValidationParameters = LexAuthService.TokenValidationParameters(jwtUserOptions),
                MapInboundClaims = false
            },
            jwtUserOptions
        );
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.ReadJwtToken(jwt);

        var newUser = JsonSerializer.Deserialize<LexAuthUser>(Base64UrlEncoder.Decode(token.RawPayload));
        _user.ShouldBeEquivalentTo(newUser);
    }
}
