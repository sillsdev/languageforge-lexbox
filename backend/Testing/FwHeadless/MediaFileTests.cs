using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using FluentAssertions.Collections;
using LexCore.Sync;
using SIL.Harmony.Core;
using Testing.ApiTests;
using Testing.Services;

namespace Testing.FwHeadless;

[Trait("Category", "Integration")]
public class MediaFileTests : ApiTestBase, IClassFixture<MediaFileTestFixture>
{
    private MediaFileTestFixture Fixture { get; init; }

    public MediaFileTests(MediaFileTestFixture fixture)
    {
        Fixture = fixture;
    }

    [Fact]
    public async Task UploadFile_WorksForAdmins()
    {
        var guid = await Fixture.PostFile("/home/rmunn/code/lexbox/data/sena-3.zip");
        guid.Should().NotBe(Guid.Empty);
        var files = await Fixture.ListFiles(Fixture.ProjectId);
        files.Should().NotBeNull();
        files.Files.Should().Contain("sena-3.zip");
    }
}
