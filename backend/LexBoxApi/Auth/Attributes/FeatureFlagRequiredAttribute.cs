using LexCore;
using Microsoft.AspNetCore.Authorization;

namespace LexBoxApi.Auth.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class FeatureFlagRequiredAttribute(FeatureFlag flag): LexboxAuthAttribute(PolicyName), IAuthorizationRequirement, IAuthorizationRequirementData
{
    public const string PolicyName = "FeatureFlagRequired";
    public bool AllowAdmin { get; set; } = false;
    public FeatureFlag Flag => flag;

    public IEnumerable<IAuthorizationRequirement> GetRequirements()
    {
        yield return this;
    }
}
