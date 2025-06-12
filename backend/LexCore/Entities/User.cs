using System.Diagnostics.CodeAnalysis;

namespace LexCore.Entities;

public class User : EntityBase
{
    public const string DefaultLocalizationCode = "en";
    public required string Name { get; set; }
    public string? Email { get; set; }
    private string _localizationCode = DefaultLocalizationCode;

    public string LocalizationCode
    {
        get => _localizationCode;
        [MemberNotNull(nameof(_localizationCode))]
        set => _localizationCode = string.IsNullOrEmpty(value) ? DefaultLocalizationCode : value;
    }

    public required bool IsAdmin { get; set; }
    public required string PasswordHash { get; set; }
    public required string Salt { get; set; }
    public int? PasswordStrength { get; set; }
    public DateTimeOffset LastActive { get; set; } = DateTimeOffset.UtcNow;
    public required bool EmailVerified { get; set; }
    public required bool CanCreateProjects { get; set; }
    public Guid? CreatedById { get; set; }
    public User? CreatedBy { get; set; }
    public List<User> UsersICreated { get; set; } = new(); // TODO: Better name? Or is this good?

    public void UpdateCreateProjectsPermission(ProjectRole role)
    {
        if (role == ProjectRole.Manager) CanCreateProjects = true;
    }
    public bool Locked { get; set; } = false;

    /// <summary>
    /// Used for legacy users
    /// </summary>
    public string? Username { get; set; }

    public string? GoogleId { get; set; }

    public List<ProjectUsers> Projects { get; set; } = new();
    public List<OrgMember> Organizations { get; set; } = [];
    public List<FeatureFlag> FeatureFlags
    {
        //even though this may not be null EF still sets it to null based on what's in the db.
        //also, EF may set the underlying field to null directly, bypassing the setter, so we need to do this.
        get => field ??= [];
        set => field = value ?? [];
    } = [];


    public bool CanLogin()
    {
        return !Locked;
    }
}
