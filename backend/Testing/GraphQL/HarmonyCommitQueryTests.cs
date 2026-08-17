using LexCore.Entities;
using LexData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SIL.Harmony.Core;
using Testing.Fixtures;

namespace Testing.GraphQL;

[Collection(nameof(TestingServicesFixture))]
[Trait("Category", "RequiresDb")]
public class HarmonyCommitQueryTests
{
    private readonly LexBoxDbContext _dbContext;
    private readonly IDbContextFactory<LexBoxDbContext> _dbContextFactory;

    public HarmonyCommitQueryTests(TestingServicesFixture testing)
    {
        var serviceProvider = testing.ConfigureServices();
        _dbContext = serviceProvider.GetRequiredService<LexBoxDbContext>();
        _dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<LexBoxDbContext>>();
    }

    // Each test gets its own project so the resolver's newest-N query only sees that test's commits,
    // keeping ordering/limit assertions isolated within the shared RequiresDb collection.
    private async Task<Project> CreateProject(ProjectType type = ProjectType.FLEx)
    {
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Code = "harmony-test-" + Guid.NewGuid().ToString("N")[..8],
            Name = "Harmony Commit Resolver Test",
            Type = type,
            RetentionPolicy = RetentionPolicy.Dev,
            IsConfidential = null,
            Users = [],
            Organizations = [],
            LastCommit = null,
        };
        _dbContext.Add(project);
        await _dbContext.SaveChangesAsync();
        return project;
    }

    private async Task SeedCommit(Guid projectId, DateTimeOffset dateTime, long counter, string? authorName)
    {
        _dbContext.Add(new ServerCommit(Guid.NewGuid())
        {
            ClientId = Guid.NewGuid(),
            HybridDateTime = new HybridDateTime(dateTime, counter),
            ProjectId = projectId,
            Metadata = new CommitMetadata { AuthorName = authorName },
        });
        await _dbContext.SaveChangesAsync();
    }

    private static string? AuthorName(ServerCommit commit) =>
        commit.Metadata.AuthorName;

    private async Task<ServerCommit[]> QueryCommits(Guid projectId, IDbContextFactory<LexBoxDbContext> dbContextFactory, int limit)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        var project = await context.Projects.Include(p => p.HarmonyCommits).FirstAsync(p => p.Id == projectId);
        return project.HarmonyCommits?.ToArray() ?? [];
    }

    [Fact]
    public async Task CommitsIncludeTheAuthorNameInMetadata()
    {
        var project = await CreateProject();
        var day = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        await SeedCommit(project.Id, day, 0, "Author1");
        await SeedCommit(project.Id, day.AddDays(1), 0, "Author2");
        await SeedCommit(project.Id, day.AddDays(2), 0, "Author3");

        var commits = await QueryCommits(project.Id, _dbContextFactory, 10);

        commits.OrderBy(c => c.HybridDateTime)
            .Select(AuthorName)
            .Should()
            .Equal("Author3", "Author2", "Author1");
    }

    [Fact]
    public async Task NullAuthorNameIsPreservedInMetadata()
    {
        var project = await CreateProject();
        await SeedCommit(project.Id, new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero), 0, null);

        var commits = await QueryCommits(project.Id, _dbContextFactory, 1);

        commits.Should().ContainSingle();
        AuthorName(commits[0]).Should().BeNull();
    }
}
