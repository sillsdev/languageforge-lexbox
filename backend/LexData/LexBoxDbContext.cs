using LexCore.Entities;
using LexData.Configuration;
using Microsoft.EntityFrameworkCore;
using SIL.Harmony.Core;

namespace LexData;

public class LexBoxDbContext(DbContextOptions<LexBoxDbContext> options, IEnumerable<ConfigureDbModel> configureDbModels) : DbContext(options)
{
    public const string CaseInsensitiveCollation = "case_insensitive";
    private readonly ConfigureDbModel[] _configureDbModels = configureDbModels.ToArray();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        modelBuilder.HasCollation(CaseInsensitiveCollation, locale: "und-u-ks-level2", provider: "icu", deterministic: false);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EntityBaseConfiguration<>).Assembly);
        foreach (var configureDbModel in _configureDbModels)
        {
            configureDbModel.Configure(modelBuilder);
        }
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
    }

    public DbSet<MediaFile> Files => Set<MediaFile>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectUsers> ProjectUsers => Set<ProjectUsers>();
    public DbSet<DraftProject> DraftProjects => Set<DraftProject>();
    public DbSet<Organization> Orgs => Set<Organization>();
    public DbSet<OrgMember> OrgMembers => Set<OrgMember>();
    public DbSet<OrgProjects> OrgProjects => Set<OrgProjects>();
    public IQueryable<ServerCommit> CrdtCommits(Guid projectId) => Set<ServerCommit>().Where(c => c.ProjectId == projectId);

    public async Task<bool> HeathCheck(CancellationToken cancellationToken)
    {
        //this will throw if we can't connect which is a valid health check response.
        await Users.CountAsync(cancellationToken);
        return true;
    }
}
