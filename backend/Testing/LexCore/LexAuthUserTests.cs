using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using LexBoxApi.Auth;
using LexCore.Auth;
using LexCore.Entities;
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
        //todo test JwtTicketDataFormat directly
        var jwtDate = DateTime.UtcNow;
        var claimsIdentity = new ClaimsIdentity(_user.GetClaims(), "test");
        var tokenHandler = new JwtSecurityTokenHandler();

        var securityTokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = "test-iss",
            Audience = "test-aud",
            NotBefore = jwtDate,
            Expires = jwtDate + TimeSpan.FromSeconds(10),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString())),
                SecurityAlgorithms.HmacSha256),
            Subject = claimsIdentity,
            Claims = new Dictionary<string, object>
            {
                { "test-claim", "test-value" }
            }
        };
        var token = tokenHandler.CreateJwtSecurityToken(securityTokenDescriptor);
        JwtTicketDataFormat.FixUpProjectClaims(token);
        var json = token.Payload.SerializeToJson();
        var newUser = JsonSerializer.Deserialize<LexAuthUser>(json);
        _user.ShouldBeEquivalentTo(newUser);
    }
}
