using LexBoxApi.Auth.Attributes;
using LexCore.Entities;

namespace LexBoxApi.GraphQL.CustomTypes;

[ObjectType]
public class OrgGqlConfiguration : ObjectType<Organization>
{
    protected override void Configure(IObjectTypeDescriptor<Organization> descriptor)
    {
        descriptor.Field(o => o.CreatedDate).IsProjected();
        descriptor.Field(o => o.Id).IsProjected(); // Needed for jwt refresh
        descriptor.Field(o => o.Id).Use<RefreshJwtOrgMembershipMiddleware>();
        //only admins can query members list and projects, custom logic is used for getById
        descriptor.Field(o => o.Members).AdminRequired();
        descriptor.Field(o => o.Projects).AdminRequired();
    }
}

/// <summary>
/// used to override some configuration for only the OrgById query
/// </summary>
[ObjectType]
public class OrgByIdGqlConfiguration : ObjectType<Organization>
{
    protected override void Configure(IObjectTypeDescriptor<Organization> descriptor)
    {
        descriptor.Name("OrgById");
        descriptor.Field(o => o.Members).Type(ListType<OrgMember>(memberDescriptor =>
        {
            memberDescriptor.Name("OrgByIdMember");
            memberDescriptor.Field(member => member.User).Type(ObjectType<User>(userDescriptor =>
            {
                userDescriptor.Name("OrgByIdUser");
                userDescriptor.BindFieldsExplicitly();
                userDescriptor.Field(u => u.Id);
                userDescriptor.Field(u => u.Name);
                userDescriptor.Field(u => u.Username);
                userDescriptor.Field(u => u.Email);
            }));
        }));
    }

    private static IOutputType ObjectType<T>(Action<IObjectTypeDescriptor<T>> configure)
    {
        return new NonNullType(new ObjectType<T>(configure));
    }

    private static IOutputType ListType<T>(Action<IObjectTypeDescriptor<T>> configure)
    {
        return new NonNullType(new ListType(ObjectType(configure)));
    }
}
