using HotChocolate.Resolvers;
using LexBoxApi.Auth;
using LexBoxApi.Auth.Attributes;
using LexBoxApi.GraphQL.CustomTypes;
using LexBoxApi.Services;
using LexCore.Auth;
using LexCore.Entities;
using LexCore.ServiceInterfaces;
using LexData;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LexBoxApi.GraphQL;

[QueryType]
public class LexQueries
{
    [UseProjection]
    [UseSorting]
    [UseFiltering]
    public async Task<IQueryable<Project>> MyProjects(
        LexAuthService lexAuthService,
        LoggedInContext loggedInContext,
        ProjectService projectService,
        IResolverContext context)
    {
        var userId = loggedInContext.User.Id;
        var myProjects = await projectService.UserProjects(userId)
            .ToLinqToDB().Project(context).ToListAsync();

        if (loggedInContext.User.IsOutOfSyncWithMyProjects(myProjects))
        {
            await lexAuthService.RefreshUser(LexAuthConstants.ProjectsClaimType);
        }

        return myProjects.AsQueryable();
    }

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    [AdminRequired]
    public IQueryable<Project> Projects(LexBoxDbContext context, bool withDeleted = false)
    {
        if (withDeleted)
        {
            return context.Projects.ToLinqToDB().IgnoreQueryFilters();
        }
        else
        {
            return context.Projects.ToLinqToDB();
        }
    }

    [UseProjection]
    [UseSorting]
    public IQueryable<DraftProject> MyDraftProjects(LoggedInContext loggedInContext, LexBoxDbContext context)
    {
        var userId = loggedInContext.User.Id;
        return context.DraftProjects.ToLinqToDB().Where(p => p.ProjectManagerId == userId);
    }

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    [AdminRequired]
    public IQueryable<DraftProject> DraftProjects(LexBoxDbContext context)
    {
        return context.DraftProjects.ToLinqToDB();
    }

    public record ProjectsByLangCodeAndOrgInput(Guid OrgId, string LangCode);
    [UseProjection]
    [UseSorting]
    public IQueryable<Project> ProjectsByLangCodeAndOrg(LoggedInContext loggedInContext, LexBoxDbContext context, IPermissionService permissionService, ProjectsByLangCodeAndOrgInput input)
    {
        if (!loggedInContext.User.IsAdmin && !permissionService.IsOrgMember(input.OrgId)) throw new UnauthorizedAccessException();
        // Convert 3-letter code to 2-letter code if relevant, otherwise leave as-is
        var langCode = LangTagConstants.ThreeToTwo.GetValueOrDefault(input.LangCode, input.LangCode);
        var query = context.Projects.ToLinqToDB().Where(p =>
            p.Organizations.Any(o => o.Id == input.OrgId) &&
            p.FlexProjectMetadata != null &&
            p.FlexProjectMetadata.WritingSystems != null &&
            p.FlexProjectMetadata.WritingSystems.VernacularWss.Any(ws =>
                ws.IsActive && (
                    ws.Tag == langCode ||
                    ws.Tag == $"qaa-x-{langCode}" ||
                    ws.Tag.StartsWith($"{langCode}-")
                )
            )
        );
        // Org admins can see all projects, everyone else can only see non-confidential
        if (!permissionService.CanEditOrg(input.OrgId))
        {
            query = query.Where(p => p.IsConfidential == false);
        }
        return query;
    }

    public record ProjectsInMyOrgInput(Guid OrgId);
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Project> ProjectsInMyOrg(LoggedInContext loggedInContext, LexBoxDbContext context, IPermissionService permissionService, ProjectsInMyOrgInput input)
    {
        if (!loggedInContext.User.IsAdmin && !permissionService.IsOrgMember(input.OrgId)) throw new UnauthorizedAccessException();
        var query = context.Projects.ToLinqToDB().Where(p => p.Organizations.Any(o => o.Id == input.OrgId));
        // Org admins can see all projects, everyone else can only see non-confidential
        if (!permissionService.CanEditOrg(input.OrgId))
        {
            query = query.Where(p => p.IsConfidential == false);
        }
        return query;
    }

    [UseSingleOrDefault]
    [UseProjection]
    public async Task<IQueryable<Project>> ProjectById(LexBoxDbContext context, IPermissionService permissionService, Guid projectId)
    {
        await permissionService.AssertCanViewProject(projectId);
        return context.Projects.ToLinqToDB().Where(p => p.Id == projectId);
    }

    public record ProjectStatus(Guid Id, bool Exists, bool Deleted, string? AccessibleCode);

    [GraphQLName("projectStatus")]
    public async Task<ProjectStatus> GetProjectStatus(LexBoxDbContext context, IPermissionService permissionService, LoggedInContext loggedInContext, Guid projectId)
    {
        var project = await context.Projects.Include(p => p.Users)
            .AsNoTracking().IgnoreQueryFilters()
            .SingleOrDefaultAsync(p => p.Id == projectId);

        if (project is null) return new ProjectStatus(projectId, false, false, null);
        if (project.DeletedDate != null) return new ProjectStatus(projectId, true, true, null);

        // explicitly check the project users in the DB, because if this endpoint returns stale results, it will result in confusion
        var hasPermission = project.Users.Any(u => u.UserId == loggedInContext.User.Id)
            || await permissionService.CanViewProject(projectId);

        if (!hasPermission) return new ProjectStatus(projectId, true, false, null);

        return new ProjectStatus(projectId, true, false, project.Code);
    }

    [UseProjection]
    public async Task<Project?> ProjectByCode(
        LexBoxDbContext dbContext,
        IPermissionService permissionService,
        LexAuthService lexAuthService,
        LoggedInContext loggedInContext,
        IResolverContext context,
        string code)
    {
        var project = await dbContext.Projects.Where(p => p.Code == code).ToLinqToDB().Project(context).SingleOrDefaultAsync();

        if (project is null) return project;

        var updatedUser = loggedInContext.User.IsOutOfSyncWithProject(project)
            ? await lexAuthService.RefreshUser(LexAuthConstants.ProjectsClaimType)
            : null;

        await permissionService.AssertCanViewProject(project.Id, updatedUser);
        return project;
    }

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Organization> Orgs(LexBoxDbContext context)
    {
        return context.Orgs.ToLinqToDB();
    }

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public async Task<IQueryable<Organization>> MyOrgs(
        LexBoxDbContext dbContext,
        LexAuthService lexAuthService,
        LoggedInContext loggedInContext,
        IResolverContext context)
    {
        var userId = loggedInContext.User.Id;
        var myOrgs = await dbContext.Orgs.Where(o => o.Members.Any(m => m.UserId == userId))
            .ToLinqToDB().Project(context).ToListAsync();

        if (loggedInContext.User.IsOutOfSyncWithMyOrgs(myOrgs))
        {
            await lexAuthService.RefreshUser(LexAuthConstants.OrgsClaimType);
        }
        return myOrgs.AsQueryable();
    }

    [UseOffsetPaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<User> UsersICanSee(UserService userService, LoggedInContext loggedInContext)
    {
        return userService.UserQueryForTypeahead(loggedInContext.User);
    }

    [UseProjection]
    [GraphQLType<OrgByIdGqlConfiguration>]
    public async Task<Organization?> OrgById(LexBoxDbContext dbContext,
        Guid orgId,
        IPermissionService permissionService,
        LexAuthService lexAuthService,
        LoggedInContext loggedInContext,
        IResolverContext context)
    {
        return await QueryOrgById(dbContext, orgId, permissionService, lexAuthService, loggedInContext, context);
    }

    [GraphQLIgnore]
    internal static async Task<Organization?> QueryOrgById(LexBoxDbContext dbContext,
        Guid orgId,
        IPermissionService permissionService,
        LexAuthService lexAuthService,
        LoggedInContext loggedInContext,
        IResolverContext context)
    {
        //todo remove this workaround once the issue is fixed
        var projectContext =
            context.GetLocalStateOrDefault<IResolverContext>("HotChocolate.Data.Projections.ProxyContext") ??
            context;
        var org = await dbContext.Orgs.Where(o => o.Id == orgId).ToLinqToDB().Project(projectContext).SingleOrDefaultAsync();
        if (org is null) return org;

        var updatedUser = loggedInContext.User.IsOutOfSyncWithOrg(org)
            ? await lexAuthService.RefreshUser(LexAuthConstants.OrgsClaimType)
            : null;

        // Site admins and org admins can see everything
        if (permissionService.CanEditOrg(orgId, updatedUser)) return org;
        // Non-admins cannot see email addresses or usernames
        org.Members?.ForEach(m =>
        {
            if (m.User is not null)
            {
                m.User.Email = null;
                m.User.Username = null;
            }
        });
        // Members and non-members alike can see all public projects plus their own
        org.Projects = org.Projects?.Where(p => p.IsConfidential == false || permissionService.IsProjectMember(p.Id, updatedUser))?.ToList() ?? [];
        if (!permissionService.IsOrgMember(orgId, updatedUser))
        {
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
        return context.Users.ToLinqToDB().OrderBy(u => u.Name);
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

    public async Task<OrgMemberDto?> OrgMemberById(LexBoxDbContext context, IPermissionService permissionService, Guid orgId, Guid userId)
    {
        // Only site admins and org admins are allowed to run this query
        if (!permissionService.CanEditOrg(orgId)) return null;

        var user = await context.Users.ToLinqToDB().Include(u => u.Organizations).Include(u => u.CreatedBy).Where(u => u.Id == userId).FirstOrDefaultAsync();
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
            FeatureFlags = user.FeatureFlags,
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
