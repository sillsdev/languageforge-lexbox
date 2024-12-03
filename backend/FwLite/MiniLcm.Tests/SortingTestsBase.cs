namespace MiniLcm.Tests;

public abstract class SortingTestsBase : MiniLcmTestBase
{
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await Api.CreateWritingSystem(WritingSystemType.Analysis,
            new WritingSystem()
            {
                Id = Guid.NewGuid(),
                Type = WritingSystemType.Analysis,
                WsId = "en",
                Name = "English",
                Abbreviation = "En",
                Font = "Arial",
                Exemplars = []
            });
        await Api.CreateWritingSystem(WritingSystemType.Vernacular,
            new WritingSystem()
            {
                Id = Guid.NewGuid(),
                Type = WritingSystemType.Vernacular,
                WsId = "en-US",
                Name = "English",
                Abbreviation = "En",
                Font = "Arial",
                Exemplars = []
            });
    }

    private Task CreateEntry(string headword)
    {
        return Api.CreateEntry(new() { LexemeForm = { { "en", headword } }, });
    }


    // ReSharper disable InconsistentNaming
    const string Ru_A= "\u0410";
    const string Ru_a = "\u0430";
    const string Ru_Б= "\u0411";
    const string Ru_б = "\u0431";
    const string Ru_В= "\u0412";
    const string Ru_в = "\u0432";
    // ReSharper restore InconsistentNaming

    [Theory]
    [InlineData("aa,ab,ac")]
    [InlineData("aa,Ab,ac")]
    [InlineData($"{Ru_a}{Ru_a},{Ru_a}{Ru_б},{Ru_a}{Ru_в}")]
    [InlineData($"{Ru_a}{Ru_a},{Ru_A}{Ru_б},{Ru_a}{Ru_в}")]
    public async Task EntriesAreSorted(string headwords)
    {
        var headwordList = headwords.Split(',');
        foreach (var headword in headwordList.OrderBy(h => Random.Shared.Next()))
        {
            await CreateEntry(headword);
        }
        var entries = await Api.GetEntries().Select(e => e.Headword()).ToArrayAsync();
        entries.Should().Equal(headwordList);
    }
}
