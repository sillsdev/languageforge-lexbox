using FluentValidation;
using MiniLcm.Models;
using Moq;

namespace MiniLcm.Tests.Validators;

/// <summary>
/// Regressions in the wrapper's own wiring only: that a write validates at all, and that it forwards its
/// arguments unchanged. Don't add tests here to sample more methods - a missing write is already a build
/// error (BeaKona auto-implements only IMiniLcmReadApi), the rules have per-validator tests, and imperative
/// rules like single-main-publication are covered by the conformance bases.
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
}
