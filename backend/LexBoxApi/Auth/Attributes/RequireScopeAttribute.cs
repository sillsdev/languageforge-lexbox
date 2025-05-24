using LexCore.Auth;
using Microsoft.AspNetCore.Authorization;

namespace LexBoxApi.Auth.Attributes;

public class RequireScopeAttribute(params LexboxAuthScope[] scopes): LexboxAuthAttribute(PolicyName), IAuthorizationRequirement, IAuthorizationRequirementData
{
    public const string PolicyName = "RequireScopePolicy";
    public LexboxAuthScope[] ValidScopes { get; } = scopes;
    /// <summary>
    /// ignore this requirement if there are other scope requirements
    /// </summary>
    public bool IsDefault { get; init; }

    /// <summary>
    /// Requires the user to have at least one of the specified scopes
    /// </summary>
    /// <param name="scope"></param>
    /// <param name="exclusive">false: may access with the LexboxApi scope, true: may not access with the LexboxApi scope</param>
    public RequireScopeAttribute(LexboxAuthScope scope, bool exclusive = false) : this(exclusive
        ? [scope]
        : [scope, LexboxAuthScope.LexboxApi])
    {

    }

    public IEnumerable<IAuthorizationRequirement> GetRequirements()
    {
        yield return this;
    }
}
