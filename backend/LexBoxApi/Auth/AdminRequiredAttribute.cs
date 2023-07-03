using System.Reflection;
using HotChocolate.Types.Descriptors;
using Microsoft.AspNetCore.Authorization;
using AuthorizeAttribute = HotChocolate.Authorization.AuthorizeAttribute;

namespace LexBoxApi.Auth;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AdminRequiredAttribute : DescriptorAttribute, IAuthorizeData
{
    public const string PolicyName = "AdminRequiredPolicy";

    public AdminRequiredAttribute()
    {
    }

    public string? Policy { get; set; } = PolicyName;

    public string? Roles { get; set; }

    public string? AuthenticationSchemes { get; set; }
    protected override void TryConfigure(IDescriptorContext context, IDescriptor descriptor, ICustomAttributeProvider element)
    {
        ApplyAttribute(context, descriptor, element, new AuthorizeAttribute(PolicyName));
    }
}
