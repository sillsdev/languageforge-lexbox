using System.Reflection;
using HotChocolate.Types.Descriptors;
using Microsoft.AspNetCore.Authorization;
using AuthorizeAttribute = HotChocolate.Authorization.AuthorizeAttribute;

namespace LexBoxApi.Auth;

public abstract class LexboxAuthAttribute : DescriptorAttribute, IAuthorizeData
{
    public LexboxAuthAttribute(string policy)
    {
        Policy = policy;
    }

    public string Policy { get; set; }

    public string? Roles { get; set; }

    public string? AuthenticationSchemes { get; set; }

    protected override void TryConfigure(IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider element)
    {
        ApplyAttribute(context, descriptor, element, new AuthorizeAttribute(Policy));
    }
}
