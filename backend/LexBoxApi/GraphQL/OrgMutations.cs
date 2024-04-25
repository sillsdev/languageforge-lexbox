using LexBoxApi.Auth;
using LexCore.Entities;
using LexCore.ServiceInterfaces;
using LexData;

namespace LexBoxApi.GraphQL;

[MutationType]
public class OrgMutations
{
    [Error<DbError>]
    [UseMutationConvention]
    [UseFirstOrDefault]
    [UseProjection]
    public async Task<IQueryable<Organization>> CreateOrganization(string name,
        LexBoxDbContext dbContext,
        LoggedInContext loggedInContext,
        IPermissionService permissionService)
    {
        permissionService.AssertCanCreateOrg();
        var userId = loggedInContext.User.Id;
        var orgId = Guid.NewGuid();
        dbContext.Orgs.Add(new Organization()
        {
            Id = orgId,
            Name = name,
            Members =
            [
                new OrgMember() { Role = OrgRole.Admin, UserId = userId }
            ]
        });
        await dbContext.SaveChangesAsync();
        return dbContext.Orgs.Where(o => o.Id == orgId);
    }
}
