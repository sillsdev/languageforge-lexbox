using MongoDB.Bson.Serialization.Attributes;

namespace LfClassicData.Entities;

public class LfProjectUser
{
    [BsonConstructor(nameof(_roleString))]
    public LfProjectUser(string roleString)
    {
        _roleString = roleString;
    }

    [BsonElement("role")]
    private readonly string _roleString;

    [BsonIgnore]
    public required ProjectRole Role
    {
        get => RoleFromString(_roleString);
        init => _roleString = RoleToString(value);
    }

    private ProjectRole RoleFromString(string role)
    {
        return role switch
        {
            "observer_with_comment" => ProjectRole.Commenter,
            "project_manager" => ProjectRole.Manager,
            "tech_support" => ProjectRole.TechSupport,
            _ => Enum.Parse<ProjectRole>(role, true)
        };
    }

    private string RoleToString(ProjectRole role)
    {
        return role switch
        {
            ProjectRole.Commenter => "observer_with_comment",
            ProjectRole.Manager => "project_manager",
            ProjectRole.TechSupport => "tech_support",
            ProjectRole.Observer => throw new NotImplementedException(),
            ProjectRole.Contributor => throw new NotImplementedException(),
            _ => role.ToString().ToLower()
        };
    }
}

public enum ProjectRole
{
    Observer,
    Commenter,
    Contributor,
    Manager,
    TechSupport,
}
