using System.Reflection;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using LexBoxApi.Auth;

namespace LexBoxApi.GraphQL.CustomTypes;

public class RefreshJwtAttribute: ObjectFieldDescriptorAttribute
{
    protected override void OnConfigure(IDescriptorContext context, IObjectFieldDescriptor descriptor, MemberInfo member)
    {
        descriptor.Use<RefreshJwtMiddleware>();
    }

    public class RefreshJwtMiddleware
    {
        private readonly FieldDelegate _next;

        public RefreshJwtMiddleware(FieldDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(IMiddlewareContext context, LoggedInContext loggedInContext, LexAuthService authService)
        {
            await _next(context);
            if (context.HasErrors)
            {
                return;
            }

            await authService.RefreshUser(loggedInContext.User.Id);
        }
    }
}
