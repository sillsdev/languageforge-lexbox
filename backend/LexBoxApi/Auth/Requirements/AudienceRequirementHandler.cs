using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.JsonWebTokens;

namespace LexBoxApi.Auth.Requirements;

public class AudienceRequirement : IAuthorizationRequirement
{
    public LexboxAudience Audience { get; }

    public AudienceRequirement(LexboxAudience audience)
    {
        Audience = audience;
    }
}

public class AudienceRequirementHandler : AuthorizationHandler<AudienceRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AudienceRequirement requirement)
    {
        var claim = context.User.FindFirst(JwtRegisteredClaimNames.Aud);
        if (claim?.Value == requirement.Audience.ToString())
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail(new AuthorizationFailureReason(this,
                $"Token does not have the required audience: {requirement.Audience}"));
        }
        return Task.CompletedTask;
    }
}
