using LexCore.Auth;

namespace LexBoxApi.GraphQL.CustomTypes;

public class LexAuthUserType: ObjectType<LexAuthUser>
{
    protected override void Configure(IObjectTypeDescriptor<LexAuthUser> descriptor)
    {
        descriptor.Ignore(u => u.GetPrincipal(""));
        descriptor.Ignore(u => u.GetClaims());
    }
}