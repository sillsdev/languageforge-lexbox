using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using FluentAssertions.Collections;
using LexCore.Entities;
using LexCore.Sync;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using SIL.Harmony.Core;
using Testing.ApiTests;
using Testing.Fixtures;
using Testing.Services;

namespace Testing.FwHeadless;

[Trait("Category", "Integration")]
public class MediaFileTests : ApiTestBase, IClassFixture<MediaFileTestFixture>
{
    private MediaFileTestFixture Fixture { get; init; }
    private FileInfo TestRepoZip = IntegrationFixture.TemplateRepoZip;
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
        files.Files.Should().Contain(TestRepoZipFilename);
        await Fixture.PutFile(TestRepoZipPath, guid);
        files = await Fixture.ListFiles(Fixture.ProjectId);
        files.Should().NotBeNull();
        files.Files.Should().Contain(TestRepoZipFilename);
    }

    [Fact]
    public async Task UploadFile_FailsForNonMembers()
    {
        var guid = await Fixture.PostFile(TestRepoZipPath, loginAs: "user", expectSuccess: false);
        guid.Should().Be(Guid.Empty);
        var files = await Fixture.ListFiles(Fixture.ProjectId, loginAs: "user");
        (files?.Files ?? []).Should().NotContain(TestRepoZipFilename);
    }

    [Fact]
    public async Task UploadFile_WorksForProjectManagers()
    {
        await Fixture.PostFile(TestRepoZipPath, loginAs: "manager", expectSuccess: true);
        var files = await Fixture.ListFiles(Fixture.ProjectId, loginAs: "manager");
        (files?.Files ?? []).Should().Contain(TestRepoZipFilename);
    }

    [Fact]
    public async Task UploadFile_WorksForProjectEditors()
    {
        await Fixture.PostFile(TestRepoZipPath, loginAs: "editor", expectSuccess: true);
        var files = await Fixture.ListFiles(Fixture.ProjectId, loginAs: "editor");
        (files?.Files ?? []).Should().Contain(TestRepoZipFilename);
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
        var expectedMetadata = new FileMetadata
        {
            Filename = TestRepoZipFilename,
            SizeInBytes = (int)expectedLength,
            Author = uploadMetadata.Author,
            License = uploadMetadata.License,
        };
        var fileId = await Fixture.PostFile(TestRepoZipPath, uploadMetadata);
        var metadata = await Fixture.GetFileMetadata(fileId);
        metadata.Should().NotBeNull();
        metadata.Should().BeEquivalentTo(expectedMetadata);
    }
}
