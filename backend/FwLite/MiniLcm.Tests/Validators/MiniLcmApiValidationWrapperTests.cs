using FluentValidation;
using MiniLcm.Models;
using MiniLcm.SyncHelpers;
using Moq;

namespace MiniLcm.Tests.Validators;

/// <summary>
/// Tests the validation wrapper's wiring through the IMiniLcmApi interface - the seam that broke in
/// #2359 / #2362, where CreateEntry's override drifted out of signature and every caller silently bound
/// to an unvalidated BeaKona forwarder. These pin that the methods we expect to validate actually do
/// (validator rules themselves have their own unit tests), and that CreateEntry now forwards its options.
/// </summary>
public class MiniLcmApiValidationWrapperTests
{
    private readonly Mock<IMiniLcmApi> _inner = new();
    private readonly IMiniLcmApi _api;

    public MiniLcmApiValidationWrapperTests()
    {
        _inner.Setup(a => a.CreateEntry(It.IsAny<Entry>(), It.IsAny<CreateEntryOptions?>()))
            .ReturnsAsync((Entry e, CreateEntryOptions? _) => e);
        _api = TestMiniLcmWrappers.CreateValidationFactory().Create(_inner.Object);
    }

    private static Entry ValidEntry() => new() { Id = Guid.NewGuid(), LexemeForm = new() { { "en", "lexeme" } } };

    [Fact]
    public async Task CreateEntry_ValidatesTheEntry()
    {
        // DeletedAt must be null (EntryValidator); an unambiguously invalid entry independent of the empty-value question.
        var invalid = ValidEntry();
        invalid.DeletedAt = DateTimeOffset.UtcNow;

        var act = () => _api.CreateEntry(invalid);

        await act.Should().ThrowAsync<ValidationException>();
        _inner.Verify(a => a.CreateEntry(It.IsAny<Entry>(), It.IsAny<CreateEntryOptions?>()), Times.Never);
    }

    [Fact]
    public async Task CreateEntry_ForwardsOptions()
    {
        var entry = ValidEntry();

        await _api.CreateEntry(entry, CreateEntryOptions.AsIs);

        // The bug in #2362 was that the drifted 1-arg override dropped options; the wrapper must pass them through.
        _inner.Verify(a => a.CreateEntry(entry, CreateEntryOptions.AsIs), Times.Once);
    }

    [Fact]
    public async Task UpdateEntry_BeforeAfter_ValidatesTheUpdatedEntry()
    {
        var after = ValidEntry();
        after.DeletedAt = DateTimeOffset.UtcNow;

        var act = () => _api.UpdateEntry(ValidEntry(), after);

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
    public async Task CreateMorphType_DoesNotValidate()
    {
        // Deliberate pass-through: sync/import writes FLEx morph types with empty names. See the wrapper's comment.
        var morphType = new MorphType { Id = Guid.NewGuid(), Kind = MorphTypeKind.Unknown, Name = new() { { "en", "" } } };

        var act = () => _api.CreateMorphType(morphType);

        await act.Should().NotThrowAsync();
        _inner.Verify(a => a.CreateMorphType(morphType), Times.Once);
    }
}
