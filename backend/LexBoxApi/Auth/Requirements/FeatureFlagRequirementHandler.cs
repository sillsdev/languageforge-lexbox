using System.Security.Claims;
using LexBoxApi.Auth.Attributes;
using LexCore;
using LexCore.Auth;
using Microsoft.AspNetCore.Authorization;

namespace LexBoxApi.Auth.Requirements;

public class FeatureFlagRequirementHandler(ILogger<FeatureFlagRequirementHandler> logger): AuthorizationHandler<FeatureFlagRequiredAttribute>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, FeatureFlagRequiredAttribute requirement)
    {
        bool success = false;
        var isAdmin = context.User.IsInRole(UserRole.admin.ToString());
        if (isAdmin && requirement.AllowAdmin)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }
        foreach (var userFlag in context.User.FindAll(LexAuthConstants.FeatureFlagsClaimType)
                     .Where(c => Enum.IsDefined(typeof(FeatureFlag), c.Value))
                     .Select(c => Enum.Parse<FeatureFlag>(c.Value)))
        {
            if (requirement.Flag == userFlag)
            {
                success = true;
                break;
            }
        }

        if (success)
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail(new AuthorizationFailureReason(this, "User does not have the feature flag "  + requirement.Flag));
            logger.LogError("User does not have the feature flag " + requirement.Flag);
        }
        return Task.CompletedTask;
    }
}
