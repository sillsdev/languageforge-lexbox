
using FwHeadless.Controllers;
using LexCore.Entities;
using LexData;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testing.Fixtures;

namespace Testing.LexCore;

[Collection(nameof(TestingServicesFixture))]
[Trait("Category", "RequiresDb")]
public class MediaFileDbTests
{
    private readonly LexBoxDbContext _lexBoxDbContext;

    public MediaFileDbTests(TestingServicesFixture testing)
    {
        var serviceProvider = testing.ConfigureServices();
        _lexBoxDbContext = serviceProvider.GetRequiredService<LexBoxDbContext>();
    }

    [Fact]
    public async Task CanGetFileMetadata()
    {
        var project = await _lexBoxDbContext.Projects.FirstAsync();
        var fileId = Guid.NewGuid();
        var mediaFile = new MediaFile
        {
            Id = fileId,
            ProjectId = project.Id,
            Filename = "testfile.txt",
            Metadata = new FileMetadata { Author = "Test Author" }
        };
        _lexBoxDbContext.Files.Add(mediaFile);
        await _lexBoxDbContext.SaveChangesAsync();
        _lexBoxDbContext.ChangeTracker.Clear();

        var result = await MediaFileMetadataController.GetFileMetadata(fileId, _lexBoxDbContext);
        var okResult = result.Result.Should().BeOfType<Ok<ApiMetadataEndpointResult>>().Subject;

        okResult.Value!.Author.Should().Be("Test Author");
    }
}
