using MiniLcm.Exceptions;
using MiniLcm.SyncHelpers;

namespace MiniLcm.Tests;

public abstract class SenseTestsBase : MiniLcmTestBase
{
    private static readonly Guid _entryId = Guid.NewGuid();
    private static readonly Guid _senseId = Guid.NewGuid();
    private static readonly Guid _nounPosId = Guid.NewGuid();

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        var nounPos = new PartOfSpeech() { Id = _nounPosId, Name = { { "en", "Noun" } } };
        await Api.CreatePartOfSpeech(nounPos);
        await Api.CreateEntry(new Entry()
        {
            Id = _entryId,
            LexemeForm = { { "en", "new-lexeme-form" } },
            Senses = [new()
            {
                Id = _senseId,
                Gloss = { { "en", "new-sense-gloss" } },
                PartOfSpeech = nounPos,
                PartOfSpeechId = _nounPosId,
            }]
        });
    }

    [Fact]
    public async Task Get_MissingSense_ReturnsNull()
    {
        var sense = await Api.GetSense(_entryId, Guid.NewGuid());
        sense.Should().BeNull();
    }

    [Fact]
    public async Task Get_ExistingSense_ReturnsSense()
    {
        var sense = await Api.GetSense(_entryId, _senseId);
        sense.Should().NotBeNull();
        sense.Gloss["en"].Should().Be("new-sense-gloss");
        sense.PartOfSpeech.Should().NotBeNull();
        sense.PartOfSpeech.Name["en"].Should().Be("Noun");
        sense.PartOfSpeech.Id.Should().Be(_nounPosId);
        sense.PartOfSpeechId.Should().Be(_nounPosId);
    }

    /// <summary>
    /// Tests that a deleted sense can be recreated.
    /// This is necessary if Chorus recreates a sense due to a merge conflict.
    /// </summary>
    [Fact]
    public async Task RecreateDeletedSense()
    {
        var initial = await Api.GetSense(_entryId, _senseId);
        initial.Should().NotBeNull();

        await Api.DeleteSense(_entryId, _senseId);
        var deleted = await Api.GetSense(_entryId, _senseId);
        deleted.Should().BeNull();

        var recreated = await Api.CreateSense(_entryId, initial);
        recreated.Should().NotBeNull();
        recreated.Should().BeEquivalentTo(initial);
    }

    [Fact]
    public async Task MoveSense_ReparentsToDifferentEntry()
    {
        // FieldWorks lets users move a sense from one entry to another. The MoveSense API must reparent
        // it (preserving identity), not just reorder it within the original entry.
        var sourceEntry = await Api.CreateEntry(new() { LexemeForm = { { "en", "source" } } });
        var destEntry = await Api.CreateEntry(new() { LexemeForm = { { "en", "dest" } } });
        var sense = await Api.CreateSense(sourceEntry.Id, new() { Id = Guid.NewGuid(), Gloss = { { "en", "moving" } } });

        await Api.MoveSense(destEntry.Id, sense.Id, new BetweenPosition(null, null));

        var senseAfter = await Api.GetSense(destEntry.Id, sense.Id);
        senseAfter.Should().NotBeNull();
        senseAfter.EntryId.Should().Be(destEntry.Id);

        var act = () => Api.GetSense(sourceEntry.Id, sense.Id);
        await act.Should().ThrowAsync<NotFoundException>();

        var sourceAfter = await Api.GetEntry(sourceEntry.Id);
        sourceAfter.Should().NotBeNull();
        sourceAfter.Senses.Should().BeEmpty();

        var destAfter = await Api.GetEntry(destEntry.Id);
        destAfter.Should().NotBeNull();
        destAfter.Senses.Should().ContainSingle(s => s.Id == sense.Id);
    }
}
