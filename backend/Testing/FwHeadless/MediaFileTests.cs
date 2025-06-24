using LexCore.Entities;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Testing.ApiTests;
using Testing.Fixtures;

namespace Testing.FwHeadless;

[Trait("Category", "Integration")]
public class MediaFileTests : ApiTestBase, IClassFixture<MediaFileTestFixture>
{
    private MediaFileTestFixture Fixture { get; init; }
    private readonly FileInfo TestRepoZip = IntegrationFixture.TemplateRepoZip;
    private string TestRepoZipFilename => TestRepoZip.Name;
    private string TestRepoZipPath => TestRepoZip.FullName;

    public MediaFileTests(MediaFileTestFixture fixture)
    {
        Fixture = fixture;
    }

    [Fact]
    public async Task UploadFile_WorksForAdmins()
    {
        var guid = await Fixture.PostFile(TestRepoZipPath);
        guid.Should().NotBe(Guid.Empty);
        var files = await Fixture.ListFiles(Fixture.ProjectId);
        files.Should().NotBeNull();
        files.Files.Should().Contain(Path.Join(guid.ToString(), TestRepoZipFilename));
        await Fixture.PutFile(TestRepoZipPath, guid);
        files = await Fixture.ListFiles(Fixture.ProjectId);
        files.Should().NotBeNull();
        files.Files.Should().Contain(Path.Join(guid.ToString(), TestRepoZipFilename));
    }

    [Fact]
    public async Task UploadFile_FailsForNonMembers()
    {
        var guid = await Fixture.PostFile(TestRepoZipPath, loginAs: "user", expectSuccess: false);
        guid.Should().Be(Guid.Empty);
        var files = await Fixture.ListFiles(Fixture.ProjectId, loginAs: "user");
        (files?.Files ?? []).Should().NotContain(Path.Join(guid.ToString(), TestRepoZipFilename));
    }

    [Fact]
    public async Task UploadFile_WorksForProjectManagers()
    {
        var guid = await Fixture.PostFile(TestRepoZipPath, loginAs: "manager", expectSuccess: true);
        var files = await Fixture.ListFiles(Fixture.ProjectId, loginAs: "manager");
        (files?.Files ?? []).Should().Contain(Path.Join(guid.ToString(), TestRepoZipFilename));
    }

    [Fact]
    public async Task UploadFile_WorksForProjectEditors()
    {
        var guid = await Fixture.PostFile(TestRepoZipPath, loginAs: "editor", expectSuccess: true);
        var files = await Fixture.ListFiles(Fixture.ProjectId, loginAs: "editor");
        (files?.Files ?? []).Should().Contain(Path.Join(guid.ToString(), TestRepoZipFilename));
    }

    [Fact]
    public async Task UploadFile_WithOutMetadata_SizeIsCorrect()
    {
        var expectedLength = TestRepoZip.Length;
        var fileId = await Fixture.PostFile(TestRepoZipPath);
        var metadata = await Fixture.GetFileMetadata(fileId);
        metadata.Should().NotBeNull();
        metadata.SizeInBytes.Should().Be((int)expectedLength);
    }

    [Fact]
    public async Task UploadFile_WithMetadata_MetadataIsCorrect()
    {
        var expectedLength = TestRepoZip.Length;
        var uploadMetadata = new FileMetadata
        {
            Author = "Test Author",
            License = MediaFileLicense.CreativeCommons,
        };
        var expectedMetadata = new ApiMetadataEndpointResult(uploadMetadata)
        {
            Filename = TestRepoZipFilename,
            SizeInBytes = (int)expectedLength,
            MimeType = "application/zip",
        };
        expectedMetadata.Author.Should().Be(uploadMetadata.Author);
        expectedMetadata.License.Should().Be(uploadMetadata.License);
        var fileId = await Fixture.PostFile(TestRepoZipPath, metadata: uploadMetadata);
        var metadata = await Fixture.GetFileMetadata(fileId);
        metadata.Should().NotBeNull();
        metadata.Should().BeEquivalentTo(expectedMetadata, opts => opts.Excluding(m => m.Sha256Hash));
    }

    [Fact]
    public async Task UploadFile_WithNoFilenameField_FilenameTakenFromUploadedFile()
    {
        var fileId = await Fixture.PostFile(TestRepoZipPath, loginAs: "admin", expectSuccess: true);
        var metadata = await Fixture.GetFileMetadata(fileId, loginAs: "admin");
        metadata.Should().NotBeNull();
        metadata.Filename.Should().Be(TestRepoZipFilename);
    }

    [Fact]
    public async Task UploadFile_TwiceWithDifferentFilenames_FilenameTakenFromFirstUpload()
    {
        var fileId = await Fixture.PostFile(TestRepoZipPath);
        var secondPath = TestRepoZipPath + ".bak";
        if (File.Exists(secondPath)) File.Delete(secondPath);
        File.Copy(TestRepoZipPath, secondPath);
        await Fixture.PutFile(secondPath, fileId);
        var metadata = await Fixture.GetFileMetadata(fileId, loginAs: "admin");
        metadata.Should().NotBeNull();
        metadata.Filename.Should().Be(TestRepoZipFilename);
        var files = await Fixture.ListFiles(Fixture.ProjectId, loginAs: "admin");
        (files?.Files ?? []).Should().Contain(Path.Join(fileId.ToString(), TestRepoZipFilename));
        (files?.Files ?? []).Should().NotContain(Path.Join(fileId.ToString(), secondPath));
    }

    [Fact]
    public async Task UploadFile_TwiceWithDifferentFilenamesButOverridingFilename_Works()
    {
        var fileId = await Fixture.PostFile(TestRepoZipPath);
        var secondPath = TestRepoZipPath + ".bak";
        if (File.Exists(secondPath)) File.Delete(secondPath);
        File.Copy(TestRepoZipPath, secondPath);
        await Fixture.PutFile(secondPath, fileId, overrideFilename: TestRepoZipFilename, expectSuccess: true);
        var metadata = await Fixture.GetFileMetadata(fileId, loginAs: "admin");
        metadata.Should().NotBeNull();
        metadata.Filename.Should().Be(TestRepoZipFilename);
    }

    [Fact]
    public async Task UploadFile_TooLarge_ThrowsError()
    {
        var dummyPath = TestRepoZipPath + ".tooLarge";
        try
        {
            if (File.Exists(dummyPath)) File.Delete(dummyPath);
            Fixture.CreateDummyFile(dummyPath, 1024 * 1024 * 120); // 120 MB
            await Fixture.PostFile(dummyPath, expectSuccess: false);
        }
        finally
        {
            if (File.Exists(dummyPath)) File.Delete(dummyPath);
        }
    }

    [Fact]
    public async Task UploadReplacementFile_TooLarge_ThrowsError()
    {
        var fileId = await Fixture.PostFile(TestRepoZipPath);
        var dummyPath = TestRepoZipPath + ".tooLarge";
        try
        {
            if (File.Exists(dummyPath)) File.Delete(dummyPath);
            Fixture.CreateDummyFile(dummyPath, 1024 * 1024 * 120); // 120 MB
            await Fixture.PutFile(dummyPath, fileId, overrideFilename: TestRepoZipFilename, expectSuccess: false);
            var metadata = await Fixture.GetFileMetadata(fileId, loginAs: "admin");
            metadata.Should().NotBeNull();
            metadata.Filename.Should().Be(TestRepoZipFilename);
        }
        finally
        {
            if (File.Exists(dummyPath)) File.Delete(dummyPath);
        }
    }

    // TODO: Test whether downloading files can use ETag to skip download if we already have the file
    // TODO: Test whether downloading files can use range requests to resume partial downloads

    // TODO: Test that metadata can be specified in form fields as well as in JSON format
    // TODO: Test that metadata in form fields will override metadata from JSON format
    // TODO: Test that metadata not in FileMetadata properties will round-trip
}
