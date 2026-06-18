using FwLiteProjectSync.Tests.Fixtures;
using LcmCrdt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MiniLcm;
using MiniLcm.Exceptions;
using MiniLcm.Models;

namespace FwLiteProjectSync.Tests;

/// <summary>
/// Lighter-weight coverage for <see cref="IgnoreNotFoundMiniLcmApi"/>: drive the CRDT API directly (no FwData,
/// no full sync), delete a parent/reference, then assert the raw API throws <see cref="NotFoundException"/>
/// while the wrapper tolerates it. The end-to-end behaviour through a real sync lives in
/// <c>SyncTests.EntryEditedInFwDataButDeletedInCrdt_SyncDoesNotThrow</c>; this class is where new
/// deleted-reference scenarios should be added.
/// </summary>
public class IgnoreNotFoundMiniLcmApiTests : IClassFixture<SyncFixture>, IAsyncLifetime
{
    private readonly SyncFixture _fixture;
    private CrdtMiniLcmApi _crdt = null!;
    private IgnoreNotFoundMiniLcmApi _wrapped = null!;

    public IgnoreNotFoundMiniLcmApiTests(SyncFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _crdt = _fixture.CrdtApi;
        _wrapped = new IgnoreNotFoundMiniLcmApi(_crdt, NullLogger.Instance);
        // A vernacular writing system is required to create/query entries.
        if ((await _crdt.GetWritingSystems()).Vernacular.Length == 0)
            await _crdt.CreateWritingSystem(new WritingSystem { Id = Guid.NewGuid(), Type = WritingSystemType.Vernacular, WsId = "en", Name = "English", Abbreviation = "en", Font = "Arial" });
    }

    public async Task DisposeAsync()
    {
        foreach (var entry in await _crdt.GetAllEntries().ToArrayAsync())
            await _crdt.DeleteEntry(entry.Id);
    }

    private Task<Entry> CreateEntry(string headword) =>
        _crdt.CreateEntry(new Entry { Id = Guid.NewGuid(), LexemeForm = { { "en", headword } } });

    [Fact]
    [Trait("Category", "Integration")]
    public async Task UpdateEntry_OnEntryDeletedInCrdt()
    {
        var entry = await CreateEntry("victim");
        await _crdt.DeleteEntry(entry.Id);
        UpdateObjectInput<Entry> Patch() => new UpdateObjectInput<Entry>().Set(e => e.CitationForm["en"], "x");

        await ((Func<Task>)(() => _crdt.UpdateEntry(entry.Id, Patch()))).Should().ThrowAsync<NotFoundException>();
        await ((Func<Task>)(() => _wrapped.UpdateEntry(entry.Id, Patch()))).Should().NotThrowAsync();
        _wrapped.IgnoredWriteCount.Should().Be(1);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CreateSense_OnEntryDeletedInCrdt()
    {
        var entry = await CreateEntry("victim");
        await _crdt.DeleteEntry(entry.Id);
        Sense NewSense() => new() { Id = Guid.NewGuid(), Gloss = { { "en", "gloss" } } };

        await ((Func<Task>)(() => _crdt.CreateSense(entry.Id, NewSense()))).Should().ThrowAsync<NotFoundException>();
        await ((Func<Task>)(() => _wrapped.CreateSense(entry.Id, NewSense()))).Should().NotThrowAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CreateComplexFormComponent_ReferencingEntryDeletedInCrdt()
    {
        var component = await CreateEntry("component");
        var complexForm = await CreateEntry("complexForm");
        await _crdt.DeleteEntry(component.Id);
        ComplexFormComponent Link() => ComplexFormComponent.FromEntries(complexForm, component);

        await ((Func<Task>)(() => _crdt.CreateComplexFormComponent(Link()))).Should().ThrowAsync<NotFoundException>();
        await ((Func<Task>)(() => _wrapped.CreateComplexFormComponent(Link()))).Should().NotThrowAsync();
    }
}
