using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using LexCore.Auth;
using LexCore.Entities;
using Shouldly;

namespace Testing.LexCore;

public class LexAuthUserTests
{
    private readonly LexAuthUser _user = new()
    {
        Id = Guid.NewGuid(),
        Email = "test@test.com",
        Role = UserRole.user,
        Projects = new[]
        {
            new AuthUserProject("test-flex", ProjectRole.Manager)
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
        var claims = _user.GetClaims().ToArray();
        var newUser = LexAuthUser.FromClaimsPrincipal(new ClaimsPrincipal(new ClaimsIdentity(claims)));
        _user.ShouldBeEquivalentTo(newUser);
    }

    [Fact]
    public void CanRoundTripClaimsThroughJwt()
    {
        var originalJwt = new JwtSecurityToken(claims: _user.GetClaims());
        var tokenHandler = new JwtSecurityTokenHandler();
        var encodedJwt = tokenHandler.WriteToken(originalJwt);
        var outputJwt = tokenHandler.ReadJwtToken(encodedJwt);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(outputJwt.Claims));
        var newUser = LexAuthUser.FromClaimsPrincipal(principal);
        _user.ShouldBeEquivalentTo(newUser);
    }
}