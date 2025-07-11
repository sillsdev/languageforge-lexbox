using System.Linq.Expressions;
using LexCore;
using LexCore.Entities;
using LexCore.Exceptions;
using LexData.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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
        builder.Property(u => u.FeatureFlags)
            .IsRequired(false)
            .HasConversion(
                flags => flags.Select(flag => flag.ToString()).ToList(),
                strs => (strs == null) ? new List<FeatureFlag>() : strs.Where(flagStr => Enum.IsDefined(typeof(FeatureFlag), flagStr)).Select(Enum.Parse<FeatureFlag>).ToList(),
                new ValueComparer<List<FeatureFlag>>(
                    (c1, c2) => (c1 ?? new()).SequenceEqual(c2 ?? new()),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()));
        builder.HasMany(user => user.Projects)
            .WithOne(projectUser => projectUser.User)
            .HasForeignKey(projectUser => projectUser.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(user => user.UsersICreated)
            .WithOne(user => user.CreatedBy)
            .HasForeignKey(user => user.CreatedById)
            // We won't allow deleting admin users until their created accounts are  reassigned
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.Organizations)
            .WithOne(m => m.User)
            .HasForeignKey(m => m.UserId)
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

    public static void AssertHasVerifiedEmailForRole(this User user, ProjectRole forRole = ProjectRole.Unknown)
    {
        // Users with verified emails are the most common case, so check that first
        if (user.Email is not null && user.EmailVerified) return;
        // Users bulk-created by admins might not have email addresses
        // Users who self-registered must verify email in all cases
        if (user.CreatedById is null) throw new ProjectMembersMustBeVerified("Member must verify email first");
        // Only project editors and observers are allowed not to have verified email addresses
        if (forRole is not ProjectRole.Editor and not ProjectRole.Observer) throw new ProjectMembersMustBeVerifiedForRole("Member must verify email before taking on this role", forRole);
    }

    public static void AssertHasVerifiedEmailForOrgRole(this User user, OrgRole forRole = OrgRole.Unknown)
    {
        // Users with verified emails are the most common case, so check that first
        if (user.Email is not null && user.EmailVerified) return;
        // Users bulk-created by admins might not have email addresses
        // Users who self-registered must verify email in all cases
        if (user.CreatedById is null) throw new OrgMembersMustBeVerified("Member must verify email first");
        // Only basic Org members are allowed not to have verified email addresses
        if (forRole != OrgRole.User) throw new OrgMembersMustBeVerifiedForRole("Member must verify email before taking on this role", forRole);
    }
}
