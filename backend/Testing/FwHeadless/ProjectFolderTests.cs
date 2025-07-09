using FwHeadless;

namespace Testing.FwHeadless;

public class ProjectFolderTests : IDisposable
{
    FwHeadlessConfig _config = new FwHeadlessConfig
    {
        LexboxUrl = "test",
        LexboxUsername = "admin",
        LexboxPassword = "pass",
        ProjectStorageRoot = Path.GetFullPath(Path.Combine(".", $"ProjectFolderTests-{Guid.NewGuid():N}")),
        MediaFileAuthority = "localhost"
    };

    public ProjectFolderTests()
    {
        Directory.CreateDirectory(_config.ProjectStorageRoot);
    }

    public void Dispose()
    {
        Directory.Delete(_config.ProjectStorageRoot, true);
    }

    [Fact]
    public void CanGetProjectFolder()
    {
        //this format must not change as there's already directories created in production matching this format
        var guidString = "66e920cd-fafe-4082-9cdb-0f91446728bf";
        var id = new Guid(guidString);
        _config.GetProjectFolder("code", id).Should().EndWith($"{Path.DirectorySeparatorChar}code-{guidString}");
    }

    [Fact]
    public void CanGetProjectFolderFromJustId()
    {
        var id = Guid.NewGuid();
        var expectedProjectFolder = _config.GetProjectFolder("code", id);
        Directory.CreateDirectory(expectedProjectFolder);
        _config.GetProjectFolder(id).Should().Be(expectedProjectFolder);
    }

    [Fact]
    public void CanGetGuidOutOfProjectFolder()
    {
        var projectCode = "test";
        var expectedGuid = Guid.NewGuid();
        var projectFolder = _config.GetProjectFolder(projectCode, expectedGuid);

        var extractedGuid = FwHeadlessConfig.IdFromProjectFolder(projectFolder);

        extractedGuid.Should().Be(expectedGuid);
    }
}
