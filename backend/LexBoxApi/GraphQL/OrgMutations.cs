using LexBoxApi.Auth;
using LexBoxApi.Auth.Attributes;
using LexBoxApi.Models.Org;
using LexCore.Entities;
using LexCore.Exceptions;
using LexCore.ServiceInterfaces;
using LexData;
using LexData.Entities;
using Microsoft.EntityFrameworkCore;

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
            ],
            Projects = []
        });
        await dbContext.SaveChangesAsync();
        return dbContext.Orgs.Where(o => o.Id == orgId);
    }

    [Error<DbError>]
    [UseMutationConvention]
    [AdminRequired]
    public async Task<Organization> DeleteOrg(Guid orgId,
        LexBoxDbContext dbContext)
    {
        var org = await dbContext.Orgs.Include(o => o.Members).FirstOrDefaultAsync(o => o.Id == orgId);
        NotFoundException.ThrowIfNull(org);

        dbContext.Remove(org);
        await dbContext.SaveChangesAsync();
        return org;
    }

    /// <summary>
    /// set the role of a member in an organization, if the member does not exist it will be created
    /// </summary>
    /// <param name="dbContext"></param>
    /// <param name="permissionService"></param>
    /// <param name="orgId"></param>
    /// <param name="role">set to null to remove the member</param>
    /// <param name="emailOrUsername">either an email or a username for the user whos membership to update</param>
    [Error<DbError>]
    [Error<NotFoundException>]
    [UseMutationConvention]
    [UseFirstOrDefault]
    [UseProjection]
    public async Task<IQueryable<Organization>> SetOrgMemberRole(
        LexBoxDbContext dbContext,
        IPermissionService permissionService,
        Guid orgId,
        OrgRole? role,
        string emailOrUsername)
    {
        var user = await dbContext.Users.FindByEmailOrUsername(emailOrUsername);
        NotFoundException.ThrowIfNull(user); // TODO: Implement inviting user
        return await ChangeOrgMemberRole(dbContext, permissionService, orgId, user.Id, role);
    }

    /// <summary>
    /// Change the role of an existing member in an organization
    /// </summary>
    /// <param name="dbContext"></param>
    /// <param name="permissionService"></param>
    /// <param name="orgId"></param>
    /// <param name="userId">ID (GUID) of the user whose membership should be updated</param>
    /// <param name="role">set to null to remove the member</param>
    [Error<DbError>]
    [Error<NotFoundException>]
    [UseMutationConvention]
    [UseFirstOrDefault]
    [UseProjection]
    public async Task<IQueryable<Organization>> ChangeOrgMemberRole(
        LexBoxDbContext dbContext,
        IPermissionService permissionService,
        Guid orgId,
        Guid userId,
        OrgRole? role)
    {
        var org = await dbContext.Orgs.Include(o => o.Members).FirstOrDefaultAsync(o => o.Id == orgId);
        NotFoundException.ThrowIfNull(org);

        permissionService.AssertCanEditOrg(org);
        var user = await dbContext.Users.FindAsync(userId);
        NotFoundException.ThrowIfNull(user);
        await UpdateOrgMemberRole(dbContext, org, role, userId);
        return dbContext.Orgs.Where(o => o.Id == orgId);
    }

    private async Task UpdateOrgMemberRole(LexBoxDbContext dbContext, Organization org, OrgRole? role, Guid userId)
    {
        var member = org.Members.FirstOrDefault(m => m.UserId == userId);
        if (member is null && role is null) return;
        if (role is not null && member is not null)
        {
            member.Role = role.Value;
        }
        else if (role is null && member is not null)
        {
            org.Members.Remove(member);
        }
        else if (role is not null && member is null)
        {
            org.Members.Add(new OrgMember { UserId = userId, Role = role.Value });
        }

        await dbContext.SaveChangesAsync();
    }

    [Error<NotFoundException>]
    [Error<DbError>]
    [Error<RequiredException>]
    [UseMutationConvention]
    [UseFirstOrDefault]
    [UseProjection]
    public async Task<IQueryable<Organization>> ChangeOrgName(ChangeOrgNameInput input,
        IPermissionService permissionService,
        LexBoxDbContext dbContext)
    {
        if (string.IsNullOrEmpty(input.Name)) throw new RequiredException("Org name cannot be empty");

        var org = await dbContext.Orgs.FindAsync(input.OrgId);
        NotFoundException.ThrowIfNull(org);
        permissionService.AssertCanEditOrg(org);

        org.Name = input.Name;
        org.UpdateUpdatedDate();
        await dbContext.SaveChangesAsync();
        return dbContext.Orgs.Where(o => o.Id == input.OrgId);
    }
}
