using FwLiteMaui.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace FwLiteMaui.Tests;

public class MauiTroubleshootingServiceTests
{
    private readonly Mock<ILauncher> _launcherMock;
    private readonly Mock<IBrowser> _browserMock;
    private readonly Mock<IShare> _shareMock;
    private readonly Mock<IOptions<FwLiteMauiConfig>> _configMock;
    private readonly MauiTroubleshootingService _service;

    public MauiTroubleshootingServiceTests()
    {
        _launcherMock = new Mock<ILauncher>();
        _browserMock = new Mock<IBrowser>();
        _shareMock = new Mock<IShare>();
        _configMock = new Mock<IOptions<FwLiteMauiConfig>>();

        var config = new FwLiteMauiConfig
        {
            BaseDataDir = "test_data_dir"
        };
        _configMock.Setup(c => c.Value).Returns(config);

        _service = new MauiTroubleshootingService(
            _configMock.Object,
            Mock.Of<ILogger<MauiTroubleshootingService>>(),
            null!, // passing null for CrdtProjectsService as we aren't testing ShareCrdtProject yet
            _launcherMock.Object,
            _browserMock.Object,
            _shareMock.Object
        );
    }

    [Fact]
    public async Task TryOpenDataDirectory_OpensBrowserWithFileUrl()
    {
        var result = await _service.TryOpenDataDirectory();

        result.Should().BeTrue();
        var expectedUri = "file://" + _configMock.Object.Value.BaseDataDir;
        _browserMock.Verify(b => b.OpenAsync(It.Is<Uri>(u => u.OriginalString == expectedUri), It.IsAny<BrowserLaunchOptions>()), Times.Once);
    }

    [Fact]
    public async Task OpenLogFile_OpensFileWithLauncher()
    {
        await _service.OpenLogFile();

        _launcherMock.Verify(l => l.OpenAsync(It.Is<OpenFileRequest>(r => r.Title == "Log File" && r.File!.FullPath.Contains("app.log"))), Times.Once);
    }
}
