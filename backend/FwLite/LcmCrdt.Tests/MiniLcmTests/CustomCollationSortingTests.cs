namespace LcmCrdt.Tests.MiniLcmTests;

public class CustomCollationSortingTests : CustomCollationSortingTestsBase
{
    private readonly MiniLcmApiFixture _fixture = new();

    protected override async Task<IMiniLcmApi> NewApi()
    {
        await _fixture.InitializeAsync();
        return _fixture.Api;
    }

    public override async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _fixture.DisposeAsync();
    }

    [Fact]
    public async Task ComplexForms_SortByWritingSystemCollation()
    {
        await Api.CreateWritingSystem(new()
        {
            Id = Guid.NewGuid(),
            Type = WritingSystemType.Vernacular,
            WsId = "de",
            Name = "German",
            Abbreviation = "De",
            Font = "Arial",
            IcuCollationRules = "&z < a",
        });
        // the default vernacular writing system is the first one
        var vernacular = (await Api.GetWritingSystems()).Vernacular.First();
        await Api.MoveWritingSystem("de", WritingSystemType.Vernacular, new(null, vernacular.WsId));

        var component = await Api.CreateEntry(new() { LexemeForm = { { "de", "base" } } });
        var apple = await Api.CreateEntry(new() { LexemeForm = { { "de", "apple" } } });
        var zebra = await Api.CreateEntry(new() { LexemeForm = { { "de", "zebra" } } });
        await Api.CreateComplexFormComponent(ComplexFormComponent.FromEntries(apple, component));
        await Api.CreateComplexFormComponent(ComplexFormComponent.FromEntries(zebra, component));

        var entry = await Api.GetEntry(component.Id);

        entry!.ComplexForms.Select(c => c.ComplexFormHeadword).Should().Equal("zebra", "apple");
    }
}
