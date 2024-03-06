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
    public static Expression<Func<User, bool>> FilterByEmailOrUsername(string email)
    {
        return user => user.Email == email || user.Username == email;
    }
    public static IQueryable<User> FilterByEmailOrUsername(this IQueryable<User> users, string? email)
    {
        if (email is null) return Enumerable.Empty<User>().AsQueryable();
        // TODO: Test that and make sure it works; if Enumerable.Empty doesn't implement IAsyncEnumerable, we'll have to do this instead:
        // if (email is null) return users.Where(u => false);
        return users.Where(FilterByEmailOrUsername(email));
    }

    public static async Task<User?> FindByEmailOrUsername(this IQueryable<User> users, string? email)
    {
        return await users.FilterByEmailOrUsername(email).FirstOrDefaultAsync();
    }
}
