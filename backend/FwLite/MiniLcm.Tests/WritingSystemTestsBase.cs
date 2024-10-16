using MiniLcm.Models;

namespace MiniLcm.Tests;

public abstract class WritingSystemTestsBase : MiniLcmTestBase
{
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await Api.CreateEntry(new Entry()
        {
            Id = Guid.NewGuid(),
            LexemeForm = { { "en", "new-lexeme-form" } },
            Senses = new List<Sense>() { new Sense() { Gloss = { { "en", "new-sense-gloss" } } } }
        });
    }

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
}
