using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;
using LexBoxApi.Auth;
using LexCore.Auth;
using LexCore.Entities;

namespace LexBoxApi.GraphQL;

public class RefreshJwtProjectMembershipMiddleware(FieldDelegate next)
{
    private readonly FieldDelegate _next = next;

    public async Task InvokeAsync(IMiddlewareContext context)
    {
        await _next(context);
        var currUser = context.Service<LoggedInContext>();
        if (currUser.User == null || currUser.User.Role == UserRole.admin) return;

        var lexAuthService = context.Service<LexAuthService>();
        var projectId = context.Parent<Project>().Id;
        if (projectId == default)
        {
            if (context.Result is not Guid projectGuid) return;
            if (projectGuid == default) return;
            projectId = projectGuid;
        } // we know we have a valid project-ID

        var currUserMembershipJwt = currUser.User.Projects.FirstOrDefault(projects => projects.ProjectId == projectId);

        if (currUserMembershipJwt is null)
        {
            // The user was probably added to the project and it's not in the token yet
            await lexAuthService.RefreshUser(currUser.User.Id, LexAuthConstants.ProjectsClaimType);
        }
        else
        {
            if (context.Result is not IEnumerable<ProjectUsers> projectUsers) return;

            var sampleProjectUser = projectUsers.FirstOrDefault();
            if (sampleProjectUser is not null && sampleProjectUser.UserId == default && (sampleProjectUser.User == null || sampleProjectUser.User.Id == default))
            {
                // User IDs don't seem to have been loaded from the DB, so we can't do anything
                return;
            }

            var currUserMembershipDb = projectUsers.FirstOrDefault(projectUser => currUser.User.Id == projectUser.UserId || currUser.User.Id == projectUser.User.Id);
            if (currUserMembershipDb is null)
            {
                // The user was probably removed from the project and it's still in the token
                await lexAuthService.RefreshUser(currUser.User.Id, LexAuthConstants.ProjectsClaimType);
            }
            else if (currUserMembershipDb.Role == default)
            {
                return; // Either the role wasn't loaded by the query (so we can't do anything) or the role is actually Unknown which means it definitely has never been changed
            }
            else if (currUserMembershipDb.Role != currUserMembershipJwt.Role)
            {
                // The user's role was changed
                await lexAuthService.RefreshUser(currUser.User.Id, LexAuthConstants.ProjectsClaimType);
            }
        }
    }
}

public class RefreshProjectMembershipInterceptor : TypeInterceptor
{
    private readonly FieldMiddlewareDefinition refreshProjectMembershipMiddleware = new(
        FieldClassMiddlewareFactory.Create<RefreshJwtProjectMembershipMiddleware>(),
        false,
        "jwt-refresh-middleware");

    public override void OnBeforeCompleteType(
        ITypeCompletionContext completionContext, DefinitionBase definition)
    {
        if (definition is ObjectTypeDefinition def && def.Name.Equals(nameof(Project)))
        {
            var idField = def.Fields.FirstOrDefault(x => x.Name.EqualsInvariantIgnoreCase(nameof(Project.Id)));
            if (idField is null) throw new InvalidOperationException("Did not find id field of project type.");
            idField.MiddlewareDefinitions.Insert(0, refreshProjectMembershipMiddleware);
            var userField = def.Fields.FirstOrDefault(x => x.Name.EqualsInvariantIgnoreCase(nameof(Project.Users)));
            if (userField is null) throw new InvalidOperationException("Did not find users field of project type.");
            userField.MiddlewareDefinitions.Insert(0, refreshProjectMembershipMiddleware);
        }
    }
}
