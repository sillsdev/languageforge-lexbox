using Microsoft.AspNetCore.Authorization;

namespace LexBoxApi.Auth;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AdminRequiredAttribute : AuthorizeAttribute
{
    public const string PolicyName = "AdminRequiredPolicy";

    public AdminRequiredAttribute() : base(PolicyName)
    {
    }
}