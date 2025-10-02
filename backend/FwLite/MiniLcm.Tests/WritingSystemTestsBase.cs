using System.Runtime.CompilerServices;
using MiniLcm.Exceptions;
using MiniLcm.SyncHelpers;
using SIL.Extensions;

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

    [Fact(Skip = "Exemplars are not used, expensive and inconsistently populated (all WSs vs single WS). So we disabled populating them.")]
    public async Task GetWritingSystems_ReturnsExemplars()
    {
        var writingSystems = await Api.GetWritingSystems();
        writingSystems.Vernacular.Should().Contain(ws => ws.Exemplars.Any());
    }

    [Fact]
    public async Task CreateWritingSystem_Works()
    {
        var ws = await Api.CreateWritingSystem(new()
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
        await Api.CreateWritingSystem(new()
        {
            Id = Guid.NewGuid(),
            WsId = wsId,
            Type = WritingSystemType.Vernacular,
            Name = "Spanish",
            Abbreviation = "Es",
            Font = "Arial"
        });
        var action = async () => await Api.CreateWritingSystem(new()
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

    [Fact]
    public async Task MoveWritingSystem_Works()
    {
        var en = await Api.GetWritingSystem("en", WritingSystemType.Vernacular);
        en.Should().NotBeNull();
        var ws1 = await Api.CreateWritingSystem(new()
        {
            Id = Guid.NewGuid(),
            WsId = "es",
            Type = WritingSystemType.Vernacular,
            Name = "Spanish",
            Abbreviation = "Es",
            Font = "Arial"
        });
        var ws2 = await Api.CreateWritingSystem(new()
        {
            Id = Guid.NewGuid(),
            WsId = "fr",
            Type = WritingSystemType.Vernacular,
            Name = "French",
            Abbreviation = "Fr",
            Font = "Arial"
        });
        var vernacularWSs = (await Api.GetWritingSystems())!.Vernacular;
        vernacularWSs.Should().BeEquivalentTo([en, ws1, ws2],
            options => options.WithStrictOrdering().Excluding(ws => ws.Order));

        //act
        await Api.MoveWritingSystem(ws2.WsId, WritingSystemType.Vernacular, new(en.WsId, ws1.WsId));

        //assert
        vernacularWSs = (await Api.GetWritingSystems())!.Vernacular;
        vernacularWSs.Should().BeEquivalentTo([en, ws2, ws1],
        options => options.WithStrictOrdering().Excluding(ws => ws.Order));
    }

    [Fact]
    public async Task InsertWritingSystem_Works()
    {
        var writingSystems = await Api.GetWritingSystems();
        var en = writingSystems.Vernacular.Single(ws => ws.WsId.Code == "en");

        //act
        var ws1 = await Api.CreateWritingSystem(new()
        {
            Id = Guid.NewGuid(),
            WsId = "es",
            Type = WritingSystemType.Vernacular,
            Name = "Spanish",
            Abbreviation = "Es",
            Font = "Arial"
        }, new BetweenPosition<WritingSystemId?>(null, en.WsId));
        var ws2 = await Api.CreateWritingSystem(new()
        {
            Id = Guid.NewGuid(),
            WsId = "fr",
            Type = WritingSystemType.Vernacular,
            Name = "French",
            Abbreviation = "Fr",
            Font = "Arial"
        }, new BetweenPosition<WritingSystemId?>(ws1.WsId, en.WsId));

        // assert
        writingSystems = await Api.GetWritingSystems();
        writingSystems.Vernacular.Should().BeEquivalentTo([ws1, ws2, en],
        // we care about the order of return, not the internal Order property
        options => options.WithStrictOrdering().Excluding(ws => ws.Order));
    }

    [Fact]
    public async Task CanChangeDefaultWritingSystem()
    {
        // arrange
        var currentDefault = await Api.GetWritingSystem(default, WritingSystemType.Vernacular);
        var writingSystems = await Api.GetWritingSystems();
        var en = writingSystems.Vernacular.Single(ws => ws.WsId.Code == "en");
        currentDefault.Should().BeEquivalentTo(en);
        writingSystems.Vernacular.First().Should().BeEquivalentTo(en);

        // act
        var es = await Api.CreateWritingSystem(new()
        {
            Id = Guid.NewGuid(),
            WsId = "es",
            Type = WritingSystemType.Vernacular,
            Name = "Spanish",
            Abbreviation = "Es",
            Font = "Arial"
        }, new BetweenPosition<WritingSystemId?>(null, en.WsId));

        //assert
        var newDefault = await Api.GetWritingSystem(default, WritingSystemType.Vernacular);
        newDefault.Should().BeEquivalentTo(es,
            options => options.Excluding(ws => ws.Order));
        writingSystems = await Api.GetWritingSystems();
        writingSystems.Vernacular.First().Should().BeEquivalentTo(es,
            options => options.Excluding(ws => ws.Order));
    }
}
