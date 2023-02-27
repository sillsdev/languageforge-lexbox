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
}