using LexCore.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.JsonWebTokens;

namespace LexBoxApi.Auth.Requirements;

public class AudienceRequirement : IAuthorizationRequirement
{
    public LexboxAudience[] ValidAudiences { get; }

    public AudienceRequirement(params LexboxAudience[] validAudiences)
    {
        ValidAudiences = validAudiences;
    }
}

public class AudienceRequirementHandler : AuthorizationHandler<AudienceRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AudienceRequirement requirement)
    {
        var claim = context.User.FindFirst(LexAuthConstants.AudienceClaimType);
        if (Enum.TryParse<LexboxAudience>(claim?.Value, out var audience) &&
            requirement.ValidAudiences.Contains(audience))
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail(new AuthorizationFailureReason(this,
                $"Token does not have the required audience: {requirement.ValidAudiences}"));
        }

        return Task.CompletedTask;
    }
}
