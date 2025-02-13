using MiniLcm.Exceptions;
using MiniLcm.Models;

namespace MiniLcm.Tests;

public abstract class WritingSystemTestsBase : MiniLcmTestBase
{
    [Fact]
    public async Task GetWritingSystems_DoesNotReturnNullOrEmpty()
    {
        var writingSystems = await Api.GetWritingSystems();
        writingSystems.Vernacular.Should().NotBeNullOrEmpty();
        writingSystems.Analysis.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetWritingSystems_ReturnsExemplars()
    {
        var writingSystems = await Api.GetWritingSystems();
        writingSystems.Vernacular.Should().Contain(ws => ws.Exemplars.Any());
    }

    [Fact]
    public async Task CreateWritingSystem_Works()
    {
        var ws = await Api.CreateWritingSystem(WritingSystemType.Vernacular,
            new()
            {
                Id = Guid.NewGuid(),
                Type = WritingSystemType.Vernacular,
                WsId = "es",
                Name = "Spanish",
                Abbreviation = "Es",
                Font = "Arial"
            });
        ws.Should().NotBeNull();
        var writingSystems = await Api.GetWritingSystems();
        writingSystems.Vernacular.Should().ContainEquivalentOf(ws);
    }

    [Fact]
    public async Task CreateWritingSystem_DoesNothingIfAlreadyExists()
    {
        WritingSystemId wsId = "es";
        await Api.CreateWritingSystem(WritingSystemType.Vernacular,
            new()
            {
                Id = Guid.NewGuid(),
                WsId = wsId,
                Type = WritingSystemType.Vernacular,
                Name = "Spanish",
                Abbreviation = "Es",
                Font = "Arial"
            });
        var action = async () => await Api.CreateWritingSystem(WritingSystemType.Vernacular,
            new()
            {
                Id = Guid.NewGuid(),
                WsId = wsId,
                Type = WritingSystemType.Vernacular,
                Name = "Spanish",
                Abbreviation = "Es",
                Font = "Arial"
            });
        await action.Should().ThrowAsync<DuplicateObjectException>();
    }

    [Fact]
    public async Task UpdateExistingWritingSystem_Works()
    {
        var writingSystems = await Api.GetWritingSystems();
        var writingSystem = writingSystems.Vernacular.First();
        var original = writingSystem.Copy();
        writingSystem.Abbreviation = "New Abbreviation";
        var updatedWritingSystem = await Api.UpdateWritingSystem(original, writingSystem);
        updatedWritingSystem.Abbreviation.Should().Be("New Abbreviation");
    }
}
