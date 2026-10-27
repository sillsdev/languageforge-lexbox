using System.Net;
using LexBoxApi.Services;
using LexCore.Config;
using LexCore.Exceptions;
using LexSyncReverseProxy;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Contrib.HttpClient;
using FluentAssertions;

namespace Testing.LexCore.Services;

public class HgServiceTests
{
    private const string LexboxHgWeb = "https://hg.lexbox";
    private const string LexboxResumable = "https://resumable.lexbox";
    private const string ManageSecret = "test-manage-secret";
    private readonly HgConfig _hgConfig;
    private readonly Mock<HttpMessageHandler> _handler = new(MockBehavior.Strict);
    private readonly HgService _hgService;

    public HgServiceTests()
    {
        _hgConfig = new HgConfig
        {
            HgWebUrl = LexboxHgWeb,
            HgCommandServer = LexboxHgWeb + "/command/",
            HgResumableUrl = LexboxResumable,
            HgResumeManageSecret = ManageSecret,
            SendReceiveDomain = LexboxHgWeb
        };

        // Default for the command server's tip lookups that follow repo changes; tests set up the manage calls they expect.
        var AllZeroHash = "0000000000000000000000000000000000000000";
        _handler.SetupAnyRequest().ReturnsResponse(AllZeroHash);

        _hgService = new HgService(new OptionsWrapper<HgConfig>(_hgConfig),
            _handler.CreateClientFactory(),
            NullLogger<HgService>.Instance);
    }

    [Theory]
    //lexbox
    [InlineData(HgType.hgWeb, LexboxHgWeb)]
    [InlineData(HgType.resumable, LexboxResumable)]
    public void DetermineProjectPrefixWorks(HgType type, string expectedUrl)
    {
        HgService.DetermineProjectUrlPrefix(type, _hgConfig).Should().Be(expectedUrl);
    }

    [Theory]
    // Valid values
    [InlineData("1630088815 0", "2021-08-27T18:26:55+0000")]
    [InlineData("1472445535 -25200", "2016-08-29T11:38:55+0700")]
    [InlineData("1472028930 14400", "2016-08-24T04:55:30-0400")]
    // hg returns "0 0" for a repo with no commits, which we want to represent as null
    [InlineData("0 0", null)]
    // Invalid values should also return null
    [InlineData("", null)]
    [InlineData(null, null)]
    [InlineData("1722581047", null)]
    [InlineData("1722581047 0 3", null)]
    [InlineData("1722581047 xyz", null)]
    [InlineData("xyz", null)]
    [InlineData("xyz 0", null)]
    [InlineData("xyz 7200", null)]
    public void HgDatesConvertedAccurately(string? input, string? expectedStr)
    {
        DateTimeOffset? expected = expectedStr == null ? null : DateTimeOffset.Parse(expectedStr);
        var actual = HgService.ConvertHgDate(input);
        actual.Should().Be(expected);
    }

    [Fact]
    public async Task InitRepoCallsManageApiWithSecret()
    {
        _handler.SetupRequest(HttpMethod.Post, LexboxResumable + "/api/manage/repos/init-test",
                r => r.Headers.GetValues("X-Manage-Secret").Single() == ManageSecret)
            .ReturnsResponse(HttpStatusCode.Created);

        await _hgService.InitRepo("init-test");

        _handler.VerifyRequest(HttpMethod.Post, LexboxResumable + "/api/manage/repos/init-test", Times.Once());
    }

    [Fact]
    public async Task InitRepoThrowsWhenRepoAlreadyExists()
    {
        _handler.SetupRequest(HttpMethod.Post, LexboxResumable + "/api/manage/repos/init-test")
            .ReturnsResponse(HttpStatusCode.Conflict);

        var act = () => _hgService.InitRepo("init-test");
        await act.Should().ThrowAsync<AlreadyExistsException>();
    }

    [Fact]
    public async Task FinishResetMapsMissingHgFolderProblem()
    {
        _handler.SetupRequest(HttpMethod.Post, LexboxResumable + "/api/manage/repos/unzip-test-no-hg/finish-reset")
            .ReturnsJsonResponse(HttpStatusCode.BadRequest, new { status = 400, title = "ZIP_MISSING_HG_FOLDER" });

        var act = () => _hgService.FinishReset("unzip-test-no-hg", new MemoryStream());
        await act.Should().ThrowAsync<ProjectResetException>();
    }

    [Fact]
    public async Task HasAbandonedTransactionsIsFalseForMissingRepo()
    {
        _handler.SetupRequest(HttpMethod.Get, LexboxResumable + "/api/manage/repos/missing")
            .ReturnsResponse(HttpStatusCode.NotFound);

        (await _hgService.HasAbandonedTransactions("missing")).Should().BeFalse();
    }

    [Fact]
    public async Task HasAbandonedTransactionsReadsRepoStatus()
    {
        _handler.SetupRequest(HttpMethod.Get, LexboxResumable + "/api/manage/repos/journal")
            .ReturnsJsonResponse(new { hasAbandonedTransactions = true, isLocked = false });

        (await _hgService.HasAbandonedTransactions("journal")).Should().BeTrue();
    }

    [Theory]
    [InlineData("-xy")]
    [InlineData("-x-y-z")]
    [InlineData("-123")]
    private async Task ProjectCodesMayNotStartWithHyphen(string code)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _hgService.InitRepo(code));
    }
}
