using System.Linq.Expressions;
using LexCore.Entities;
using LexCore.Exceptions;
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
        builder.HasMany(user => user.UsersICreated)
            .WithOne(user => user.CreatedBy)
            .HasForeignKey(user => user.CreatedById)
            // We won't allow deleting admin users until their created accounts are  reassigned
            .OnDelete(DeleteBehavior.Restrict);
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

    public static void AssertHasVerifiedEmailForRole(this User user, ProjectRole forRole = ProjectRole.Unknown)
    {
        // Users with verified emails are the most common case, so check that first
        if (user.Email is not null && user.EmailVerified) return;
        // Users bulk-created by admins might not have email addresses
        if (user.CreatedById is not null) {
            // Project editors (basic role) are allowed not to have email addresses
            if (forRole == ProjectRole.Editor) return;
            // BUT if they are to be project managers, they must have email addresses *and* those must be verified
            throw new ProjectMembersMustBeVerifiedForRole("Member must verify email first", forRole);
        } else {
            // Users who self-registered must verify email in all cases
            throw new ProjectMembersMustBeVerified("Member must verify email first");
        }
    }
}
