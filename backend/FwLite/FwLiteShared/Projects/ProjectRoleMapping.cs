using LcmCrdt;
using LexCore.Entities;

namespace FwLiteShared.Projects;

public static class ProjectRoleMapping
{
    public static UserProjectRole ToUserProjectRole(this ProjectRole role) =>
        role switch
        {
            ProjectRole.Observer => UserProjectRole.Observer,
            ProjectRole.Editor => UserProjectRole.Editor,
            ProjectRole.Manager => UserProjectRole.Manager,
            ProjectRole.Commenter => UserProjectRole.Commenter,
            ProjectRole.EditorLite => UserProjectRole.EditorLite,
            _ => UserProjectRole.Unknown
        };

    public static ProjectRole ToProjectRole(this UserProjectRole role) =>
        role switch
        {
            UserProjectRole.Observer => ProjectRole.Observer,
            UserProjectRole.Editor => ProjectRole.Editor,
            UserProjectRole.Manager => ProjectRole.Manager,
            UserProjectRole.Commenter => ProjectRole.Commenter,
            UserProjectRole.EditorLite => ProjectRole.EditorLite,
            _ => ProjectRole.Unknown
        };
}
