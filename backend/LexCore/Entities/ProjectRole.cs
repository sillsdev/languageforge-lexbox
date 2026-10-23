namespace LexCore.Entities;

public enum ProjectRole
{
    Unknown = 0,
    // Admin = 1,
    Manager = 2,
    Editor = 3,
    Observer = 4,
    /// <summary>
    /// Read-only like <see cref="Observer"/>, but can also comment. FieldWorks Lite only.
    /// </summary>
    Commenter = 5,
}
