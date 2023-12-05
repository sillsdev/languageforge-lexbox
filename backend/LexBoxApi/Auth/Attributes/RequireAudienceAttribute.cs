using LexCore.Auth;
using Microsoft.AspNetCore.Authorization;

namespace LexBoxApi.Auth.Attributes;

public class RequireAudienceAttribute(params LexboxAudience[] audiences)
    : LexboxAuthAttribute(PolicyName), IAuthorizationRequirement, IAuthorizationRequirementData
{
    public const string PolicyName = "RequireAudiencePolicy";
    /// <param name="audience">audience allowed to access this endpoint</param>
    /// <param name="exclusive">when false the default audience is also allowed, when true the default audience is not allowed</param>
    public RequireAudienceAttribute(LexboxAudience audience, bool exclusive) : this(exclusive
        ? [audience]
        : [audience, LexboxAudience.LexboxApi])
    {
    }

    public LexboxAudience[] ValidAudiences { get; } = audiences;

    public IEnumerable<IAuthorizationRequirement> GetRequirements()
    {
        yield return this;
    }
}

public class AllowAnyAudienceAttribute : LexboxAuthAttribute
{
    public const string PolicyName = "AllowAnyAudiencePolicy";

    public AllowAnyAudienceAttribute() : base(PolicyName)
    {
    }
}
