using LcmCrdt.Changes;

namespace LcmCrdt.Tests.Changes;

public class WritingSystemChangeTests(MiniLcmApiFixture fixture) : IClassFixture<MiniLcmApiFixture>
{
    [Fact]
    public async Task CreatingTheSameWritingSystemShouldResultInOnlyOne()
    {
        var writingSystem = new WritingSystem()
        {
            Id = Guid.NewGuid(),
            WsId = "es",
            Abbreviation = "Es",
            Font = "test",
            Name = "Spanish",
            Type = WritingSystemType.Analysis
        };
        await fixture.Api.CreateWritingSystem(writingSystem);

        //added via sync
        await fixture.DataModel.AddChange(Guid.NewGuid(),
            new CreateWritingSystemChange(writingSystem, Guid.NewGuid(), 2));
        var writingSystems = await fixture.Api.GetWritingSystems();
        writingSystems.Analysis.Should().ContainSingle(ws => ws.WsId == "es");
    }

    [Fact]
    public async Task Creating2SimilarWritingSystemsWorks()
    {
        var writingSystem = new WritingSystem()
        {
            Id = Guid.NewGuid(),
            WsId = "de",
            Abbreviation = "De",
            Font = "test",
            Name = "German",
            Type = WritingSystemType.Analysis
        };
        await fixture.Api.CreateWritingSystem(writingSystem);
        await fixture.Api.CreateWritingSystem(writingSystem with {Type = WritingSystemType.Vernacular, Id = Guid.NewGuid()});
        var writingSystems = await fixture.Api.GetWritingSystems();
        writingSystems.Analysis.Should().ContainSingle(ws => ws.WsId == "de");
        writingSystems.Vernacular.Should().ContainSingle(ws => ws.WsId == "de");
    }
}
