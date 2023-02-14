using LexData.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LexData.Entities;

public class User : EntityBase
{
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required bool IsAdmin { get; set; }
    public required string PasswordHash { get; set; }
    public required string Salt { get; set; }

    /// <summary>
    /// Used for legacy users
    /// </summary>
    public string? Username { get; set; }
    
    public required List<ProjectUsers> Projects { get; set; }
}

public class UserEntityConfiguration : EntityBaseConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasMany(user => user.Projects)
            .WithOne(projectUser => projectUser.User)
            .HasForeignKey(projectUser => projectUser.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}