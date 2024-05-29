using OpenIddict.Abstractions;
using OpenIddict.Server;

namespace LexBoxApi.Auth;

/// <summary>
/// the MSAL library makes requests with the scope parameter, which is invalid, this attempts to remove the scope before it's rejected
/// </summary>
public sealed class ScopeRequestFixer : IOpenIddictServerHandler<OpenIddictServerEvents.ValidateTokenRequestContext>
{
    public static OpenIddictServerHandlerDescriptor Descriptor { get; }
        = OpenIddictServerHandlerDescriptor.CreateBuilder<OpenIddictServerEvents.ValidateTokenRequestContext>()
            .UseSingletonHandler<ScopeRequestFixer>()
            .SetOrder(OpenIddictServerHandlers.Exchange.ValidateResourceOwnerCredentialsParameters.Descriptor.Order + 1)
            .SetType(OpenIddictServerHandlerType.Custom)
            .Build();

    public ValueTask HandleAsync(OpenIddictServerEvents.ValidateTokenRequestContext context)
    {
        if (!string.IsNullOrEmpty(context.Request.Scope) && (context.Request.IsAuthorizationCodeGrantType() ||
                                                             context.Request.IsDeviceCodeGrantType()))
        {
            context.Request.Scope = null;
        }

        return default;
    }
}
