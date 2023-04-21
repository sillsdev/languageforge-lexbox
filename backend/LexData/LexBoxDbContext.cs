using LexCore.Entities;
using LexData.Configuration;
using Microsoft.EntityFrameworkCore;

namespace LexData;

public class LexBoxDbContext: DbContext
{
    public LexBoxDbContext(DbContextOptions<LexBoxDbContext> options): base(options)
    {
        
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EntityBaseConfiguration<>).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectUsers> ProjectUsers => Set<ProjectUsers>();

    public async Task<bool> HeathCheck(CancellationToken cancellationToken)
    {
        //this will throw if we can't connect which is a valid health check response.
        await Users.CountAsync(cancellationToken);
        return true;
    }
}