using HotChocolate.Resolvers;
using LexBoxApi.Auth;
using LexBoxApi.Auth.Attributes;
using LexBoxApi.GraphQL.CustomTypes;
using LexCore.Auth;
using LexCore.Entities;
using LexCore.ServiceInterfaces;
using LexData;
using Microsoft.EntityFrameworkCore;

namespace LexBoxApi.GraphQL;

[QueryType]
public class LexQueries
{
    [UseProjection]
    [UseSorting]
    public IQueryable<Project> MyProjects(LoggedInContext loggedInContext, LexBoxDbContext context)
    {
        var userId = loggedInContext.User.Id;
        return context.Projects.Where(p => p.Users.Select(u => u.UserId).Contains(userId));
    }

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    [AdminRequired]
    public IQueryable<Project> Projects(LexBoxDbContext context, bool withDeleted = false)
    {
        if (withDeleted)
        {
            return context.Projects.IgnoreQueryFilters();
        }
        else
        {
            return context.Projects;
        }
    }

    [UseProjection]
    [UseSorting]
    public IQueryable<DraftProject> MyDraftProjects(LoggedInContext loggedInContext, LexBoxDbContext context)
    {
        var userId = loggedInContext.User.Id;
        return context.DraftProjects.Where(p => p.ProjectManagerId == userId);
    }

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    [AdminRequired]
    public IQueryable<DraftProject> DraftProjects(LexBoxDbContext context)
    {
        return context.DraftProjects;
    }

    [UseSingleOrDefault]
    [UseProjection]
    public async Task<IQueryable<Project>> ProjectById(LexBoxDbContext context, IPermissionService permissionService, Guid projectId)
    {
        await permissionService.AssertCanViewProject(projectId);
        return context.Projects.Where(p => p.Id == projectId);
    }

    [UseSingleOrDefault]
    [UseProjection]
    public async Task<IQueryable<Project>> ProjectByCode(LexBoxDbContext context, IPermissionService permissionService, string code)
    {
        await permissionService.AssertCanViewProject(code);
        return context.Projects.Where(p => p.Code == code);
    }

    [UseSingleOrDefault]
    [UseProjection]
    [AdminRequired]
    public IQueryable<DraftProject> DraftProjectByCode(LexBoxDbContext context, IPermissionService permissionService, string code)
    {
        return context.DraftProjects.Where(p => p.Code == code);
    }

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Organization> Orgs(LexBoxDbContext context)
    {
        return context.Orgs;
    }

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Organization> MyOrgs(LexBoxDbContext context, LoggedInContext loggedInContext)
    {
        var userId = loggedInContext.User.Id;
        return context.Orgs.Where(o => o.Members.Any(m => m.UserId == userId));
    }

    [UseProjection]
    [GraphQLType<OrgByIdGqlConfiguration>]
    public async Task<Organization?> OrgById(LexBoxDbContext dbContext, Guid orgId, IPermissionService permissionService, IResolverContext context)
    {
        var org = await dbContext.Orgs.Where(o => o.Id == orgId).AsNoTracking().Project(context).SingleOrDefaultAsync();
        if (org is null) return org;
        // Site admins and org admins can see everything
        if (permissionService.CanEditOrg(orgId)) return org;
        // Non-admins cannot see email addresses or usernames
        org.Members?.ForEach(m => { if (m.User is not null) { m.User.Email = null; m.User.Username = null; } });
        // Members can see all public projects plus their own
        if (permissionService.IsOrgMember(orgId))
        {
            org.Projects = org.Projects.Where(p => p.IsConfidential == false || permissionService.CanSyncProject(p.Id)).ToList();
        }
        else
        {
            org.Projects = org.Projects?.Where(p => p.IsConfidential == false).ToList() ?? [];
            // Non-members also cannot see membership, only org admins
            org.Members = org.Members?.Where(m => m.Role == OrgRole.Admin).ToList() ?? [];
        }
        return org;
    }

    [UseOffsetPaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    [AdminRequired]
    public IQueryable<User> Users(LexBoxDbContext context)
    {
        //default order by, can be overwritten by the gql query
        return context.Users.OrderBy(u => u.Name);
    }

    public async Task<MeDto?> Me(LexBoxDbContext context, LoggedInContext loggedInContext)
    {
        var userId = loggedInContext.User.Id;
        var user = await context.Users.FindAsync(userId);
        if (user == null) return null;
        return new MeDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Locale = user.LocalizationCode
        };
    }

    public async Task<OrgMemberDto?> OrgMemberById(LexBoxDbContext context, LoggedInContext loggedInContext, Guid orgId, Guid userId)
    {
        var requestingUserId = loggedInContext.User.Id;
        var requestingUser = await context.Users.Include(u => u.Organizations).Where(u => u.Id == requestingUserId).FirstOrDefaultAsync();
        if (requestingUser is null) return null;

        var isOrgAdmin = requestingUser.Organizations.Any(om => om.OrgId == orgId && om.UserId == requestingUserId && om.Role == OrgRole.Admin);
        var allowed = isOrgAdmin || requestingUser.IsAdmin;
        if (!allowed) return null;

        var user = await context.Users.Include(u => u.Organizations).Include(u => u.CreatedBy).Where(u => u.Id == userId).FirstOrDefaultAsync();
        if (user is null) return null;

        var userInOrg = user.Organizations.Any(om => om.OrgId == orgId);
        if (!userInOrg) return null;

        return new OrgMemberDto
        {
            Id = user.Id,
            CreatedDate = user.CreatedDate,
            UpdatedDate = user.UpdatedDate,
            LastActive = user.LastActive,
            Name = user.Name,
            Email = user.Email,
            Username = user.Username,
            LocalizationCode = user.LocalizationCode,
            EmailVerified = user.EmailVerified,
            IsAdmin = user.IsAdmin,
            Locked = user.Locked,
            CanCreateProjects = user.CanCreateProjects,
            CreatedBy = user.CreatedBy is null ? null : new OrgMemberDtoCreatedBy { Id = user.CreatedBy.Id, Name = user.CreatedBy.Name },
        };
    }

    public LexAuthUser MeAuth(LoggedInContext loggedInContext)
    {
        return loggedInContext.User;
    }

    public LexAuthUser TestingThrowsError()
    {
        throw new Exception("This is a test error");
    }
}
