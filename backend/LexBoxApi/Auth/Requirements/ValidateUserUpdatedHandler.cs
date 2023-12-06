using LexBoxApi.Auth.Attributes;
using LexBoxApi.Services;
using LexData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace LexBoxApi.Auth.Requirements;

public class ValidateUserUpdatedHandler(IHttpContextAccessor httpContextAccessor, ILogger<ValidateUserUpdatedHandler> logger) : AuthorizationHandler<RequireCurrentUserInfoAttribute>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, RequireCurrentUserInfoAttribute requirement)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var user = httpContext?.RequestServices.GetRequiredService<LoggedInContext>().MaybeUser;
        if (user is null) return;
        var userService = httpContext!.RequestServices.GetRequiredService<UserService>();
        var actualUpdatedDate = await userService.GetUserUpdatedDate(user.Id);
        if (actualUpdatedDate != user.UpdatedDate)
        {
            logger.LogInformation("User has been updated since login, {UpdatedDate} != {ActualUpdatedDate}", user.UpdatedDate, actualUpdatedDate);
            context.Fail(new AuthorizationFailureReason(this, "User has been updated since login"));
        }
        else
        {
            context.Succeed(requirement);
        }
    }
}
