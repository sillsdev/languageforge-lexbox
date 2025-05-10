using LcmCrdt.Changes;

namespace LcmCrdt.Tests.Changes;

public class WritingSystemChangeTests(MiniLcmApiFixture fixture) : IClassFixture<MiniLcmApiFixture>
{
    [Fact]
    public async Task CreatingTheSameWritingSystemShouldWork()
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
        await fixture.Api.CreateWritingSystem(WritingSystemType.Analysis, writingSystem);

        //added via sync
        await fixture.DataModel.AddChange(Guid.NewGuid(),
            new CreateWritingSystemChange(writingSystem, WritingSystemType.Analysis, Guid.NewGuid(), 2));
        var writingSystems = await fixture.Api.GetWritingSystems();
        writingSystems.Analysis.Should().Contain(ws => ws.WsId == "de");
    }
}
