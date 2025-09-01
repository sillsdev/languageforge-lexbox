using LexBoxApi.Auth.Attributes;
using LexCore.Auth;
using Microsoft.AspNetCore.Authorization;

namespace LexBoxApi.Auth.Requirements;

public class ScopeRequirementHandler(ILogger<ScopeRequirementHandler> logger): AuthorizationHandler<RequireScopeAttribute>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RequireScopeAttribute requirement)
    {
        if (requirement.IsDefault && context.Requirements.OfType<RequireScopeAttribute>().Count() > 1)
        {
            //because this is the default requirement, we can skip it when there are other scope requirements
            context.Succeed(requirement);
            return Task.CompletedTask;
        }
        bool scopeClaimExists = false;
        foreach (var claim in context.User.FindAll(LexAuthConstants.ScopeClaimType))
        {
            scopeClaimExists = true;
            foreach (var validScope in requirement.ValidScopes)
            {
                if (LexAuthUser.HasScope(claim.Value, validScope))
                {
                    context.Succeed(requirement);
                    return Task.CompletedTask;
                }
            }
        }

        //fallback to checking the audience if we didn't find a scope, matching LexAuthUser.HasScope
        if (!scopeClaimExists)
        {
            foreach (var claim in context.User.FindAll(LexAuthConstants.AudienceClaimType))
            {
                foreach (var validScope in requirement.ValidScopes)
                {
                    if (LexAuthUser.HasScope(claim.Value, validScope))
                    {
                        context.Succeed(requirement);
                        return Task.CompletedTask;
                    }
                }
            }
        }

        context.Fail(new AuthorizationFailureReason(this, "User does not have the required scope"));
        logger.LogError("User does not have the required scope {Scope}", requirement.ValidScopes);
        return Task.CompletedTask;
    }
}
