using System.Linq.Expressions;
using LexCore.Entities;
using LexData.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LexData.Entities;

public class UserEntityConfiguration : EntityBaseConfiguration<User>
{
    public override void Configure(EntityTypeBuilder<User> builder)
    {
        base.Configure(builder);
        builder.Property(u => u.LocalizationCode).HasDefaultValue(User.DefaultLocalizationCode);
        builder.Property(u => u.Username).UseCollation(LexBoxDbContext.CaseInsensitiveCollation);
        builder.HasIndex(u => u.Username).IsUnique();
        builder.Property(u => u.Email).UseCollation(LexBoxDbContext.CaseInsensitiveCollation);
        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasMany(user => user.Projects)
            .WithOne(projectUser => projectUser.User)
            .HasForeignKey(projectUser => projectUser.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}


public static class UserEntityExtensions
{
    public static Expression<Func<User, bool>> FilterByEmailOrUsername(string emailOrUsername)
    {
        return user => user.Email == emailOrUsername || user.Username == emailOrUsername;
    }
    public static IQueryable<User> FilterByEmailOrUsername(this IQueryable<User> users, string emailOrUsername)
    {
        return users.Where(FilterByEmailOrUsername(emailOrUsername));
    }

    public static async Task<User?> FindByEmailOrUsername(this IQueryable<User> users, string emailOrUsername)
    {
        return await users.FilterByEmailOrUsername(emailOrUsername).FirstOrDefaultAsync();
    }

    public static bool HasVerifiedEmailForRole(this User user, ProjectRole forRole = ProjectRole.Unknown)
    {
        // Users bulk-created by admins might not have email addresses, and that's okay
        // BUT if they are to be project managers, they must have verified email addresses
        if (forRole == ProjectRole.Editor && user.CreatedById is not null) return true;
        // Otherwise, we can simply use the EmailVerified property
        return user.Email is not null && user.EmailVerified;
    }
}
