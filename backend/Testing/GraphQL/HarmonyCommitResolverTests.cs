using System.Text.Json;
using FluentAssertions;
using LexBoxApi.GraphQL.CustomTypes;
using LexCore.Entities;
using LexData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SIL.Harmony;
using SIL.Harmony.Core;
using Testing.Fixtures;

namespace Testing.GraphQL;

[Collection(nameof(TestingServicesFixture))]
[Trait("Category", "RequiresDb")]
public class HarmonyCommitResolverTests
{
    private readonly LexBoxDbContext _dbContext;
    private readonly IDbContextFactory<LexBoxDbContext> _dbContextFactory;

    public HarmonyCommitResolverTests(TestingServicesFixture testing)
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

    [Fact]
    public async Task ReturnsCommitsNewestFirstWithCounterTiebreak()
    {
        var project = await CreateProject();
        var day = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        // Seed in an order that is neither the expected output nor its reverse, so passing proves the
        // resolver sorts by HybridDateTime (date then counter) rather than by insertion/Id order.
        await SeedCommit(project.Id, day.AddDays(1), 0, "SameDayLowCounter");
        await SeedCommit(project.Id, day, 0, "Oldest");
        await SeedCommit(project.Id, day.AddDays(1), 1, "SameDayHighCounter");

        var commits = await HarmonyCommitResolver.GetHarmonyCommits(project, _dbContextFactory, 10, default);

        commits.Select(AuthorName).Should().Equal("SameDayHighCounter", "SameDayLowCounter", "Oldest");
    }

    [Fact]
    public async Task CapsResultsAtLimitKeepingTheNewest()
    {
        var project = await CreateProject();
        var day = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        // Shuffled insertion order so "last two inserted" != the expected newest-two (c4, c3).
        foreach (var i in new[] { 2, 4, 0, 3, 1 }) await SeedCommit(project.Id, day.AddDays(i), 0, $"c{i}");

        var commits = await HarmonyCommitResolver.GetHarmonyCommits(project, _dbContextFactory, 2, default);

        commits.Select(AuthorName).Should().Equal("c4", "c3");
    }

    [Fact]
    public async Task ClampsLimitToAtLeastOne()
    {
        var project = await CreateProject();
        var day = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        await SeedCommit(project.Id, day, 0, "a");
        await SeedCommit(project.Id, day.AddDays(1), 0, "b");

        var commits = await HarmonyCommitResolver.GetHarmonyCommits(project, _dbContextFactory, 0, default);

        commits.Should().ContainSingle();
        AuthorName(commits[0]).Should().Be("b");
    }

    [Fact]
    public async Task NullAuthorNameIsPreservedInMetadata()
    {
        var project = await CreateProject();
        await SeedCommit(project.Id, new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero), 0, null);

        var commits = await HarmonyCommitResolver.GetHarmonyCommits(project, _dbContextFactory, 1, default);

        var metadata = commits.Should().ContainSingle().Subject.Metadata;
        metadata.AuthorName.Should().BeNull();
    }

    [Fact]
    public async Task UnknownTypeProjectIsQueriedLikeFlex()
    {
        // Unknown is the other type allowed by the resolver's guard (Type is Unknown or FLEx).
        var project = await CreateProject(ProjectType.Unknown);
        await SeedCommit(project.Id, new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero), 0, "author");

        var commits = await HarmonyCommitResolver.GetHarmonyCommits(project, _dbContextFactory, 50, default);

        commits.Should().ContainSingle();
    }

    [Fact]
    public async Task NonFlexProjectReturnsEmpty()
    {
        // The empty result comes from the Type guard's early return, not from an empty query.
        var project = await CreateProject(ProjectType.WeSay);
        await SeedCommit(project.Id, new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero), 0, "ignored");

        var commits = await HarmonyCommitResolver.GetHarmonyCommits(project, _dbContextFactory, 50, default);

        commits.Should().BeEmpty();
    }
}
