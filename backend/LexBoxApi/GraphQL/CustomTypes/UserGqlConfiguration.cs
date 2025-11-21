using HotChocolate.Data.Filters;
using HotChocolate.Data.Sorting;
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
        descriptor.Ignore(u => u.GoogleId);

        descriptor.Field(u => u.Email).AdminRequired();
        descriptor.Field(u => u.EmailVerified).AdminRequired();
        descriptor.Field(u => u.Username).AdminRequired();
        descriptor.Field(u => u.Projects).AdminRequired();
        descriptor.Field(u => u.Locked).AdminRequired();
    }
}

[ObjectType]
public class UserFilterType : FilterInputType<User>
{
    protected override void Configure(
        IFilterInputTypeDescriptor<User> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor.AllowOr();
        descriptor.Field(t => t.Name);
        descriptor.Field(t => t.Email);
        descriptor.Field(t => t.IsAdmin);
        descriptor.Field(t => t.CreatedById);
        descriptor.Field(t => t.Username);
        descriptor.Field(t => t.Id);
        descriptor.Field(t => t.FeatureFlags);
        descriptor.Field(t => t.CreatedDate);
        descriptor.Field(t => t.Locked);
        descriptor.Field(t => t.LastActive);
    }
}

[ObjectType]
public class UserSortType : SortInputType<User>
{
    protected override void Configure(
        ISortInputTypeDescriptor<User> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor.Field(t => t.Name);
        descriptor.Field(t => t.Email);
        descriptor.Field(t => t.Username);
        descriptor.Field(t => t.CreatedDate);
    }
}
