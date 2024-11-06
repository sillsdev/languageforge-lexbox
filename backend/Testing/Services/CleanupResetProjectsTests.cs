using LexBoxApi.Services;
using LexCore.Utils;
using Shouldly;

namespace Testing.Services;

public class CleanupResetProjectsTests
{
    [Fact]
    public void ResetRegexCanFindTimestampFromResetRepoName()
    {
        var date = DateTimeOffset.UtcNow;
        var repoName = HgService.DeletedRepoName("test", HgService.ResetSoftDeleteSuffix(date));
        var match = HgService.ResetProjectsRegex().Match(repoName);
        match.Success.ShouldBeTrue();
        match.Groups[1].Value.ShouldBe(FileUtils.ToTimestamp(date));
    }

    [Fact]
    public void CanGetDateFromResetRepoName()
    {
        var expected = DateTimeOffset.Now;
        var repoName = HgService.DeletedRepoName("test", HgService.ResetSoftDeleteSuffix(expected));
        var actual = HgService.GetResetDate(repoName);
        actual.ShouldNotBeNull();
        TruncateToMinutes(actual.Value).ShouldBe(TruncateToMinutes(expected));
    }

    private DateTimeOffset TruncateToMinutes(DateTimeOffset date)
    {
        return new DateTimeOffset(date.Year, date.Month, date.Day, date.Hour, date.Minute, 0, date.Offset);
    }

    [Theory]
    [InlineData("grobish-test-flex__2023-11-29T16-52-38__reset", "2023-11-29T16-52-38")]
    public void ResetRegexCanFindTimestamp(string repoName, string timestamp)
    {
        var match = HgService.ResetProjectsRegex().Match(repoName);
        match.Success.ShouldBeTrue();
        match.Groups[1].Value.ShouldBe(timestamp);
    }

    [Theory]
    [InlineData("grobish-test-flex__2023-11-29T16-52-38")]
    //even if the code has a pattern that would match the reset with timestamp it mush be at the end
    [InlineData("code-with-bad-name-2023-11-29T16-52-38__reset__2023-11-29T16-52-38")]
    public void ResetRegexDoesNotMatchNonResets(string repoName)
    {
        var match = HgService.ResetProjectsRegex().Match(repoName);
        match.Success.ShouldBeFalse();
    }

}
