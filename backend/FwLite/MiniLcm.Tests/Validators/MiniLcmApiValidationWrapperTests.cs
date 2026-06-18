using FluentValidation;
using MiniLcm.SyncHelpers;
using Moq;

namespace MiniLcm.Tests.Validators;

/// <summary>
/// Tests the validation wrapper's wiring through the IMiniLcmApi interface - the seam that broke in
/// #2362 - not the validator rules (those have their own unit tests). Two things are worth pinning:
/// that methods we expect to validate actually do (so a silently-dead override like #2362's is caught),
/// and that methods which deliberately DON'T validate despite having a validator (CreateEntry,
/// CreatePublication, CreateMorphType) keep passing through - so a well-meaning "fix" that turns
/// validation on, and breaks sync of empty FLEx values, trips a red test instead.
/// </summary>
public class MiniLcmApiValidationWrapperTests
{
    private readonly Mock<IMiniLcmApi> _inner = new();
    private readonly IMiniLcmApi _api;

    public MiniLcmApiValidationWrapperTests()
    {
        _api = TestMiniLcmWrappers.CreateValidationFactory().Create(_inner.Object);
    }

    private static Entry ValidEntry() => new() { Id = Guid.NewGuid(), LexemeForm = new() { { "en", "lexeme" } } };

    // An entry the EntryValidator rejects: a MultiString that carries an empty value (NoEmptyValues).
    // This is the shape that routinely arrives from FLEx during sync/import.
    private static Entry EntryWithEmptyValue() => new()
    {
        Id = Guid.NewGuid(),
        LexemeForm = new() { { "en", "lexeme" } },
        CitationForm = new() { { "en", "" } },
    };

    [Fact]
    public async Task UpdateEntry_BeforeAfter_ValidatesTheUpdatedEntry()
    {
        var act = () => _api.UpdateEntry(ValidEntry(), EntryWithEmptyValue());

        await act.Should().ThrowAsync<ValidationException>();
        _inner.Verify(a => a.UpdateEntry(It.IsAny<Entry>(), It.IsAny<Entry>(), It.IsAny<IMiniLcmApi?>()), Times.Never);
    }

    [Fact]
    public async Task CreateSense_ValidatesTheSense()
    {
        var sense = new Sense { Id = Guid.NewGuid(), Gloss = new() { { "en", "" } } };

        var act = () => _api.CreateSense(Guid.NewGuid(), sense);

        await act.Should().ThrowAsync<ValidationException>();
        _inner.Verify(a => a.CreateSense(It.IsAny<Guid>(), It.IsAny<Sense>(), It.IsAny<BetweenPosition?>()), Times.Never);
    }

    [Fact]
    public async Task CreateEntry_DoesNotValidate_SoEmptyFlexValuesReachStorage()
    {
        var entry = EntryWithEmptyValue();

        var act = () => _api.CreateEntry(entry);

        await act.Should().NotThrowAsync();
        _inner.Verify(a => a.CreateEntry(entry, It.IsAny<CreateEntryOptions?>()), Times.Once);
    }

    [Fact]
    public async Task CreatePublication_DoesNotValidate()
    {
        var publication = new Publication { Id = Guid.NewGuid(), Name = new() { { "en", "" } } };

        var act = () => _api.CreatePublication(publication);

        await act.Should().NotThrowAsync();
        _inner.Verify(a => a.CreatePublication(publication), Times.Once);
    }

    [Fact]
    public async Task CreateMorphType_DoesNotValidate()
    {
        var morphType = new MorphType { Id = Guid.NewGuid(), Kind = MorphTypeKind.Unknown, Name = new() { { "en", "" } } };

        var act = () => _api.CreateMorphType(morphType);

        await act.Should().NotThrowAsync();
        _inner.Verify(a => a.CreateMorphType(morphType), Times.Once);
    }
}
