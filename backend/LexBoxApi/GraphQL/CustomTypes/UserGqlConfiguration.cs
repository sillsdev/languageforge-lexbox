using LexBoxApi.Auth.Attributes;
using LexCore.Entities;

namespace LexBoxApi.GraphQL.CustomTypes;

[ObjectType]
public class UserGqlConfiguration : ObjectType<User>
{
    protected override void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor.Ignore(u => u.Salt);
        descriptor.Ignore(u => u.PasswordHash);
        descriptor.Ignore(u => u.CanLogin());

        descriptor.Field(u => u.Email).AdminRequired();
        descriptor.Field(u => u.EmailVerified).AdminRequired();
        descriptor.Field(u => u.Username).AdminRequired();
        descriptor.Field(u => u.Projects).AdminRequired();
        descriptor.Field(u => u.Locked).AdminRequired();
    }
}
