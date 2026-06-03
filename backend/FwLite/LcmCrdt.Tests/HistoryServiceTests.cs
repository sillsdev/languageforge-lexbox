using Microsoft.EntityFrameworkCore;
using MiniLcm.Tests.AutoFakerHelpers;
using Soenneker.Utils.AutoBogus;

namespace LcmCrdt.Tests;

public class HistoryServiceTests(MiniLcmApiFixture fixture) : IClassFixture<MiniLcmApiFixture>
{
    private static readonly AutoFaker AutoFaker = new(AutoFakerDefault.MakeConfig(["en"]));

    private LcmCrdtDbContext DbContext => fixture.DbContext;
    private IMiniLcmApi Api => fixture.Api;
    private HistoryService Service => fixture.GetService<HistoryService>();

    private async Task TagLatestCommitAuthor(string? authorName)
    {
        var commit = await DbContext.Commits.OrderByDescending(c => c.HybridDateTime.DateTime).FirstAsync();
        commit.Metadata.AuthorName = authorName;
        DbContext.Entry(commit).Property(c => c.Metadata).IsModified = true;
        await DbContext.SaveChangesAsync();
    }

    private async Task SeedCommitWithAuthor(string? authorName)
    {
        await Api.CreateEntry(await AutoFaker.EntryReadyForCreation(Api));
        await TagLatestCommitAuthor(authorName);
    }

    [Fact]
    public async Task GetAuthors_ReturnsDistinctAuthorsIncludingMissing()
    {
        await SeedCommitWithAuthor("FieldWorks");
        await SeedCommitWithAuthor("Tim");
        await SeedCommitWithAuthor("Tim"); // duplicate -> still distinct
        await SeedCommitWithAuthor(null);

        var authors = await Service.GetAuthors();
        authors.Should().Contain("FieldWorks");
        authors.Should().Contain("Tim");
        authors.Should().Contain((string?)null);
        authors.Where(a => a == "Tim").Should().HaveCount(1);
    }

    [Fact]
    public async Task ProjectActivity_FilterByAuthorName_ReturnsOnlyMatching()
    {
        await SeedCommitWithAuthor("FieldWorks");
        await SeedCommitWithAuthor("Tim");
        await SeedCommitWithAuthor("Alice");

        var result = await Service.ProjectActivity(0, 100, new ProjectActivityFilter(AuthorName: "Tim")).ToListAsync();
        result.Should().NotBeEmpty();
        result.Should().AllSatisfy(a => a.Metadata.AuthorName.Should().Be("Tim"));
    }

    [Fact]
    public async Task ProjectActivity_FilterExcludeFieldWorks_OmitsFieldWorksCommits()
    {
        await SeedCommitWithAuthor("FieldWorks");
        await SeedCommitWithAuthor("Tim");
        await SeedCommitWithAuthor(null);

        var result = await Service.ProjectActivity(0, 100, new ProjectActivityFilter(ExcludeFieldWorks: true)).ToListAsync();
        result.Should().NotBeEmpty();
        result.Should().AllSatisfy(a => a.Metadata.AuthorName.Should().NotBe("FieldWorks"));
        // Includes null-author commits (they are not FieldWorks)
        result.Select(a => a.Metadata.AuthorName).Should().Contain((string?)null);
    }

    [Fact]
    public async Task ProjectActivity_FilterAuthorMissing_ReturnsOnlyMissingAuthorCommits()
    {
        await SeedCommitWithAuthor("FieldWorks");
        await SeedCommitWithAuthor("Tim");
        await SeedCommitWithAuthor(null);

        var result = await Service.ProjectActivity(0, 100, new ProjectActivityFilter(AuthorMissing: true)).ToListAsync();
        result.Should().NotBeEmpty();
        result.Should().AllSatisfy(a => a.Metadata.AuthorName.Should().BeNull());
    }

    [Fact]
    public async Task ProjectActivity_NullFilter_BehavesLikeNoFilter()
    {
        await SeedCommitWithAuthor("FieldWorks");
        await SeedCommitWithAuthor("Tim");

        var unfiltered = await Service.ProjectActivity(0, 100).ToListAsync();
        var nullFilter = await Service.ProjectActivity(0, 100, null).ToListAsync();
        nullFilter.Should().HaveCount(unfiltered.Count);
    }
}
