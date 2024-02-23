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
    public static Expression<Func<User, bool>> FilterByEmail(string email)
    {
        return user => user.Email == email || user.Username == email;
    }
    public static IQueryable<User> FilterByEmail(this IQueryable<User> users, string email)
    {
        return users.Where(FilterByEmail(email));
    }

    public static async Task<User?> FindByEmail(this IQueryable<User> users, string email)
    {
        return await users.FilterByEmail(email).FirstOrDefaultAsync();
    }
}
