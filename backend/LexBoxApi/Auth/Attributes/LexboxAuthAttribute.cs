using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HotChocolate.Types.Descriptors;
using Microsoft.AspNetCore.Authorization;
using AuthorizeAttribute = HotChocolate.Authorization.AuthorizeAttribute;

namespace LexBoxApi.Auth.Attributes;

public abstract class LexboxAuthAttribute : DescriptorAttribute, IAuthorizeData
{
    public LexboxAuthAttribute(string policy)
    {
        _policy = policy;
    }

    private string _policy;

    [AllowNull]
    public string Policy
    {
        get => _policy;
        set => _policy = value ?? throw new ArgumentNullException(nameof(value));
    }

    public string? Roles { get; set; }

    public string? AuthenticationSchemes { get; set; }

    protected override void TryConfigure(IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider element)
    {
        ApplyAttribute(context, descriptor, element, new AuthorizeAttribute(Policy));
    }
}
