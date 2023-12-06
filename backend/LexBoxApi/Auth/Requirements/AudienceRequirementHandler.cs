using LexBoxApi.Auth.Attributes;
using LexCore.Auth;
using Microsoft.AspNetCore.Authorization;

namespace LexBoxApi.Auth.Requirements;

public class AudienceRequirementHandler(ILogger<AudienceRequirementHandler> logger) : AuthorizationHandler<RequireAudienceAttribute>
{

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
        RequireAudienceAttribute requirement)
    {
        var claim = context.User.FindFirst(LexAuthConstants.AudienceClaimType);
        if (Enum.TryParse<LexboxAudience>(claim?.Value, out var audience) &&
            requirement.ValidAudiences.Contains(audience))
        {
            context.Succeed(requirement);
        }
        else
        {
            logger.LogInformation("Token does not have the required audience: [{Audience}] not in {ValidAudiences}", claim?.Value, string.Join(',', requirement.ValidAudiences));
            context.Fail(new AuthorizationFailureReason(this,
                $"Token does not have the required audience: {requirement.ValidAudiences}"));
        }

        return Task.CompletedTask;
    }
}
