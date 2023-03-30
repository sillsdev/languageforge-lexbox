namespace LexData.Redmine;

public partial class RmRole
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int? Position { get; set; }

    public bool? Assignable { get; set; }

    public int Builtin { get; set; }

    public string? Permissions { get; set; }

    public string IssuesVisibility { get; set; } = null!;

    public string UsersVisibility { get; set; } = null!;

    public string TimeEntriesVisibility { get; set; } = null!;

    public bool? AllRolesManaged { get; set; }

    public string? Settings { get; set; }
}
