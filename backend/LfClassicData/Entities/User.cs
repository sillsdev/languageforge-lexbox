using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LfClassicData.Entities;

internal class User : EntityDocument<User>
{
    [BsonConstructor(nameof(_roleString))]
    public User(string role)
    {
        _roleString = role;
    }

    public required string Name { get; init; }
    public required string Username { get; init; }
    public required string Email { get; init; }
    /// <summary>
    /// bcrypt hashed password
    /// </summary>
    public required string Password { get; init; }
    public required DateTimeOffset DateCreated { get; init; }

    [BsonElement("role")]
    private readonly string _roleString;

    [BsonIgnore]
    public required UserRole Role
    {
        get => UserRoleFromString(_roleString);
        init => _roleString = UserRoleToString(value);
    }

    /// <summary>
    /// a list of project Id's (not project codes) this user is in
    /// </summary>
    public required List<string> Projects { get; init; }

    private UserRole UserRoleFromString(string role)
    {
        return role switch
        {
            "system_admin" => UserRole.SystemAdmin,
            "user" => UserRole.User,
            _ => UserRole.User
        };
    }

    private string UserRoleToString(UserRole userRole)
    {
        return userRole switch
        {
            UserRole.SystemAdmin => "system_admin",
            UserRole.User => "user",
            _ => throw new NotSupportedException($"""user role "{userRole}" is not supported""")
        };
    }

    public required bool Active { get; init; }
}

public enum UserRole
{
    SystemAdmin,
    User
}
