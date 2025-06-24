using LexCore.Entities;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Testing.ApiTests;
using Testing.Fixtures;
using System.Net;

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
        var (guid, result) = await Fixture.PostFile(TestRepoZipPath);
        result.StatusCode.Should().Be(HttpStatusCode.Created);
        guid.Should().NotBe(Guid.Empty);
        var (files, listResult) = await Fixture.ListFiles(Fixture.ProjectId);
        listResult.StatusCode.Should().Be(HttpStatusCode.OK);
        files.Should().NotBeNull();
        files.Files.Should().Contain(Path.Join(guid.ToString(), TestRepoZipFilename));
        await Fixture.PutFile(TestRepoZipPath, guid);
        (files, listResult) = await Fixture.ListFiles(Fixture.ProjectId);
        listResult.StatusCode.Should().Be(HttpStatusCode.OK);
        files.Should().NotBeNull();
        files.Files.Should().Contain(Path.Join(guid.ToString(), TestRepoZipFilename));
    }

    [Fact]
    public async Task UploadFile_FailsForNonMembers()
    {
        var (guid, result) = await Fixture.PostFile(TestRepoZipPath, loginAs: "user");
        guid.Should().Be(Guid.Empty);
        result.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var (files, listResult) = await Fixture.ListFiles(Fixture.ProjectId, loginAs: "user");
        listResult.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (files?.Files ?? []).Should().NotContain(Path.Join(guid.ToString(), TestRepoZipFilename));
    }

    [Fact]
    public async Task UploadFile_WorksForProjectManagers()
    {
        var (guid, result) = await Fixture.PostFile(TestRepoZipPath, loginAs: "manager");
        result.StatusCode.Should().Be(HttpStatusCode.Created);
        var (files, listResult) = await Fixture.ListFiles(Fixture.ProjectId, loginAs: "manager");
        listResult.StatusCode.Should().Be(HttpStatusCode.OK);
        (files?.Files ?? []).Should().Contain(Path.Join(guid.ToString(), TestRepoZipFilename));
    }

    [Fact]
    public async Task UploadFile_WorksForProjectEditors()
    {
        var (guid, result) = await Fixture.PostFile(TestRepoZipPath, loginAs: "editor");
        result.StatusCode.Should().Be(HttpStatusCode.Created);
        var (files, listResult) = await Fixture.ListFiles(Fixture.ProjectId, loginAs: "editor");
        listResult.StatusCode.Should().Be(HttpStatusCode.OK);
        (files?.Files ?? []).Should().Contain(Path.Join(guid.ToString(), TestRepoZipFilename));
    }

    [Fact]
    public async Task UploadFile_WithOutMetadata_SizeIsCorrect()
    {
        var expectedLength = TestRepoZip.Length;
        var (fileId, result) = await Fixture.PostFile(TestRepoZipPath);
        result.StatusCode.Should().Be(HttpStatusCode.Created);
        var (metadata, mResult) = await Fixture.GetFileMetadata(fileId);
        mResult.StatusCode.Should().Be(HttpStatusCode.OK);
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
        var (fileId, result) = await Fixture.PostFile(TestRepoZipPath, metadata: uploadMetadata);
        result.StatusCode.Should().Be(HttpStatusCode.Created);
        var (metadata, mResult) = await Fixture.GetFileMetadata(fileId);
        mResult.StatusCode.Should().Be(HttpStatusCode.OK);
        metadata.Should().NotBeNull();
        metadata.Should().BeEquivalentTo(expectedMetadata, opts => opts.Excluding(m => m.Sha256Hash));
    }

    [Fact]
    public async Task UploadFile_WithNoFilenameField_FilenameTakenFromUploadedFile()
    {
        var (fileId, result) = await Fixture.PostFile(TestRepoZipPath, loginAs: "admin");
        result.StatusCode.Should().Be(HttpStatusCode.Created);
        var (metadata, mResult) = await Fixture.GetFileMetadata(fileId, loginAs: "admin");
        mResult.StatusCode.Should().Be(HttpStatusCode.OK);
        metadata.Should().NotBeNull();
        metadata.Filename.Should().Be(TestRepoZipFilename);
    }

    [Fact]
    public async Task UploadFile_TwiceWithDifferentFilenames_FilenameTakenFromFirstUpload()
    {
        var (fileId, result) = await Fixture.PostFile(TestRepoZipPath);
        result.StatusCode.Should().Be(HttpStatusCode.Created);
        var secondPath = TestRepoZipPath + ".bak";
        if (File.Exists(secondPath)) File.Delete(secondPath);
        File.Copy(TestRepoZipPath, secondPath);
        result = await Fixture.PutFile(secondPath, fileId);
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        var (metadata, mResult) = await Fixture.GetFileMetadata(fileId, loginAs: "admin");
        mResult.StatusCode.Should().Be(HttpStatusCode.OK);
        metadata.Should().NotBeNull();
        metadata.Filename.Should().Be(TestRepoZipFilename);
        var (files, listResult) = await Fixture.ListFiles(Fixture.ProjectId, loginAs: "admin");
        listResult.StatusCode.Should().Be(HttpStatusCode.OK);
        (files?.Files ?? []).Should().Contain(Path.Join(fileId.ToString(), TestRepoZipFilename));
        (files?.Files ?? []).Should().NotContain(Path.Join(fileId.ToString(), secondPath));
    }

    [Fact]
    public async Task UploadFile_TwiceWithDifferentFilenamesButOverridingFilename_Works()
    {
        var (fileId, result) = await Fixture.PostFile(TestRepoZipPath);
        result.StatusCode.Should().Be(HttpStatusCode.Created);
        var secondPath = TestRepoZipPath + ".bak";
        if (File.Exists(secondPath)) File.Delete(secondPath);
        File.Copy(TestRepoZipPath, secondPath);
        result = await Fixture.PutFile(secondPath, fileId, overrideFilename: TestRepoZipFilename);
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        var (metadata, mResult) = await Fixture.GetFileMetadata(fileId, loginAs: "admin");
        mResult.StatusCode.Should().Be(HttpStatusCode.OK);
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
            Fixture.CreateDummyFile(dummyPath, 1024 * 1024 * 24); // 24 MB
            var (guid, result) = await Fixture.PostFile(dummyPath);
            result.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
            guid.Should().BeEmpty();
        }
        finally
        {
            if (File.Exists(dummyPath)) File.Delete(dummyPath);
        }
    }

    [Fact]
    public async Task UploadReplacementFile_TooLarge_ThrowsError()
    {
        var (fileId, result) = await Fixture.PostFile(TestRepoZipPath);
        result.StatusCode.Should().Be(HttpStatusCode.Created);
        var dummyPath = TestRepoZipPath + ".tooLarge";
        try
        {
            if (File.Exists(dummyPath)) File.Delete(dummyPath);
            Fixture.CreateDummyFile(dummyPath, 1024 * 1024 * 120); // 120 MB
            result = await Fixture.PutFile(dummyPath, fileId, overrideFilename: TestRepoZipFilename);
            result.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
            var (metadata, mResult) = await Fixture.GetFileMetadata(fileId, loginAs: "admin");
            mResult.StatusCode.Should().Be(HttpStatusCode.OK);
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
