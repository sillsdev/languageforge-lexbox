using Microsoft.AspNetCore.Authorization;

namespace LexBoxApi.Auth.Attributes;

/// <summary>
/// validates the updated date of the jwt against the database, should be used to make a jwt expire if the user is updated
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireCurrentUserInfoAttribute: Attribute, IAuthorizationRequirement, IAuthorizationRequirementData
{
    public IEnumerable<IAuthorizationRequirement> GetRequirements()
    {
        yield return this;
    }
}
