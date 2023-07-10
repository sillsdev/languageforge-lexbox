namespace LexCore.Entities;

public class User : EntityBase
{
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required bool IsAdmin { get; set; }
    public required string PasswordHash { get; set; }
    public required string Salt { get; set; }
    public required bool EmailVerified { get; set; }
    public required string? EmailVerificationToken { get; set; }
    /// <summary>
    /// The user's previous e-mail address
    /// Only saved until the new address has been verified
    /// </summary>
    public string? PreviousEmail { get; set; }

    /// <summary>
    /// Used for legacy users
    /// </summary>
    public string? Username { get; set; }

    public List<ProjectUsers> Projects { get; set; } = new();
}
