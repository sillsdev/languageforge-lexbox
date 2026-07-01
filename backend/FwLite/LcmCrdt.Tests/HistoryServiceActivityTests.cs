using LcmCrdt.Changes;
using LcmCrdt.Changes.Entries;
using LcmCrdt.Utils;
using Microsoft.EntityFrameworkCore;
using MiniLcm.Tests.AutoFakerHelpers;
using SIL.Harmony.Core;
using Soenneker.Utils.AutoBogus;
using SystemTextJsonPatch;

namespace LcmCrdt.Tests;

public class HistoryServiceActivityTests : IAsyncLifetime, IAsyncDisposable
{
    private static readonly AutoFaker AutoFaker = new(AutoFakerDefault.MakeConfig(["en"]));
    private MiniLcmApiFixture _fixture = null!;

    private HistoryService Service => _fixture.GetService<HistoryService>();
    private DataModel DataModel => _fixture.DataModel;
    private Guid ClientId => _fixture.GetService<CurrentProjectService>().ProjectData.ClientId;

    public async Task InitializeAsync()
    {
        _fixture = MiniLcmApiFixture.Create();
        await _fixture.InitializeAsync();
    }

    public async Task DisposeAsync() => await _fixture.DisposeAsync();

    async ValueTask IAsyncDisposable.DisposeAsync() => await DisposeAsync();

    [Fact]
    public async Task ListActivityAuthors_ReturnsDistinctAuthorsWithCounts()
    {
        await AddEntryCommit(new CommitMetadata { AuthorName = "Alice", AuthorId = "alice-id" });
        await AddEntryCommit(new CommitMetadata { AuthorName = "Alice", AuthorId = "alice-id" });
        await AddEntryCommit(new CommitMetadata { AuthorName = "Bob", AuthorId = "bob-id" });

        var authors = await Service.ListActivityAuthors();

        authors.Should().Contain(a => a.AuthorId == "alice-id" && a.AuthorName == "Alice" && a.CommitCount == 2);
        authors.Should().Contain(a => a.AuthorId == "bob-id" && a.AuthorName == "Bob" && a.CommitCount == 1);
    }

    [Fact]
    public async Task ListActivityChangeTypes_IncludesCreateEntry()
    {
        await AddEntryCommit(new CommitMetadata { AuthorName = "Alice", AuthorId = "alice-id" });
        await AddEntryCommit(new CommitMetadata { AuthorName = "Alice", AuthorId = "alice-id" });
        await AddNewPublicationCommit(new CommitMetadata { AuthorName = "Alice", AuthorId = "alice-id" });
        await AddNewPartOfSpeechCommit(new CommitMetadata { AuthorName = "Alice", AuthorId = "alice-id" });

        var changeTypes = await Service.ListActivityChangeTypes();

        changeTypes.Should().Contain(t => t.Key == nameof(CreateEntryChange) && t.CommitCount >= 2);
        changeTypes.Should().Contain(t => t.Key == nameof(CreatePublicationChange) && t.CommitCount >= 1);
        changeTypes.Should().Contain(t => t.Key == nameof(CreatePartOfSpeechChange) && t.CommitCount >= 1);
    }

    [Fact]
    public async Task ProjectActivity_FiltersByAuthorId()
    {
        await AddEntryCommit(new CommitMetadata { AuthorName = "Alice", AuthorId = "alice-id" });
        await AddEntryCommit(new CommitMetadata { AuthorName = "Bob", AuthorId = "bob-id" });

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery(AuthorFilterKeys: ["alice-id"])).ToArrayAsync();

        activities.Should().OnlyContain(a => a.Metadata.AuthorId == "alice-id");
        activities.Should().HaveCountGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task ProjectActivity_AuthorFilterKeys_ExcludesUnselectedAuthors()
    {
        await AddEntryCommit(new CommitMetadata { AuthorName = "Alice", AuthorId = "alice-id" });
        await AddEntryCommit(new CommitMetadata { AuthorName = "FieldWorks" });

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery(AuthorFilterKeys: ["alice-id"])).ToArrayAsync();

        activities.Should().NotContain(a => a.Metadata.AuthorName == "FieldWorks");
        activities.Should().Contain(a => a.Metadata.AuthorName == "Alice");
    }

    [Fact]
    public async Task ProjectActivity_SortsOldestFirst()
    {
        await AddEntryCommit(new CommitMetadata { AuthorName = "First", AuthorId = "first" }, "alpha");
        await Task.Delay(5);
        await AddEntryCommit(new CommitMetadata { AuthorName = "Second", AuthorId = "second" }, "beta");

        var activities = await Service.ProjectActivity(0, 1000, new ActivityQuery(Sort: ActivitySort.OldestFirst)).ToArrayAsync();
        var firstIndex = Array.FindIndex(activities, a => a.Metadata.AuthorId == "first");
        var secondIndex = Array.FindIndex(activities, a => a.Metadata.AuthorId == "second");
        firstIndex.Should().BeGreaterThanOrEqualTo(0);
        secondIndex.Should().BeGreaterThan(firstIndex);
    }

    [Fact]
    public async Task ProjectActivity_SyncedNewestFirst_PlacesUnsyncedFirst()
    {
        var syncedCommit = await AddEntryCommit(new CommitMetadata { AuthorName = "Synced", AuthorId = "synced" }, "synced-entry");
        await SetSyncDate(syncedCommit.Id, new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero));
        await AddEntryCommit(new CommitMetadata { AuthorName = "Unsynced", AuthorId = "unsynced" }, "unsynced-entry");

        var activities = await Service.ProjectActivity(0, 1000, new ActivityQuery(Sort: ActivitySort.SyncedNewestFirst)).ToArrayAsync();
        var commitAuthors = activities.Select(a => a.Metadata.AuthorId).Where(a => a is not null);
        commitAuthors.Should().ContainInOrder(["unsynced", "synced"]);
    }

    [Fact]
    public async Task ProjectActivity_SyncedOldestFirst_PlacesUnsyncedLast()
    {
        var syncedCommit = await AddEntryCommit(new CommitMetadata { AuthorName = "Synced", AuthorId = "synced" }, "synced-entry");
        await SetSyncDate(syncedCommit.Id, new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero));
        await AddEntryCommit(new CommitMetadata { AuthorName = "Unsynced", AuthorId = "unsynced" }, "unsynced-entry");

        var activities = await Service.ProjectActivity(0, 1000, new ActivityQuery(Sort: ActivitySort.SyncedOldestFirst)).ToArrayAsync();
        var commitAuthors = activities.Select(a => a.Metadata.AuthorId).Where(a => a is not null);
        commitAuthors.Should().ContainInOrder(["synced", "unsynced"]);
    }

    [Fact]
    public async Task ProjectActivity_PaginationRespectsFilters()
    {
        await AddEntryCommit(new CommitMetadata { AuthorName = "Alice", AuthorId = "alice-id" });
        await AddEntryCommit(new CommitMetadata { AuthorName = "Alice", AuthorId = "alice-id" });
        await AddEntryCommit(new CommitMetadata { AuthorName = "Bob", AuthorId = "bob-id" });

        var page = await Service.ProjectActivity(0, 1, new ActivityQuery(AuthorFilterKeys: ["alice-id"])).ToArrayAsync();

        page.Should().HaveCount(1);
        page[0].Metadata.AuthorId.Should().Be("alice-id");
    }

    [Fact]
    public async Task ProjectActivity_FiltersByChangeTypeKeys()
    {
        await AddEntryCommit(new CommitMetadata { AuthorName = "Alice", AuthorId = "alice-id" });
        await AddNewPublicationCommit(new CommitMetadata { AuthorName = "Alice", AuthorId = "alice-id" });

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery(ChangeTypeKeys: [nameof(CreateEntryChange)])).ToArrayAsync();

        activities.Should().OnlyContain(a => a.ChangeTypes.Contains(nameof(CreateEntryChange)));
        activities.Should().HaveCountGreaterThanOrEqualTo(1);
        activities.Should().NotContain(a => a.ChangeTypes.Contains(nameof(CreatePublicationChange)));
    }

    [Fact]
    public async Task ProjectActivity_ChangeTypeKeys_FiltersMultipleTypes()
    {
        await AddEntryCommit(new CommitMetadata { AuthorName = "Alice", AuthorId = "alice-id" });
        await AddNewPublicationCommit(new CommitMetadata { AuthorName = "Alice", AuthorId = "alice-id" });
        await AddNewPartOfSpeechCommit(new CommitMetadata { AuthorName = "Alice", AuthorId = "alice-id" });
        (await Service.ProjectActivity(0, 100, new ActivityQuery()).ToArrayAsync())
            .Should().Contain(a => a.ChangeTypes.Contains(nameof(CreatePartOfSpeechChange)));

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery(ChangeTypeKeys: [nameof(CreateEntryChange), nameof(CreatePublicationChange)])).ToArrayAsync();

        activities.Should().HaveCountGreaterThanOrEqualTo(2); // the entry + publication commits, so the filter can't pass vacuously on an empty result
        activities.Should().OnlyContain(a => a.ChangeTypes.Any(t => t == nameof(CreateEntryChange) || t == nameof(CreatePublicationChange)));
        activities.Should().NotContain(a => a.ChangeTypes.Contains(nameof(CreatePartOfSpeechChange)));
    }

    [Fact]
    public async Task ProjectActivity_IncludesChangeTypes()
    {
        await AddEntryCommit(new CommitMetadata { AuthorName = "Alice", AuthorId = "alice-id" });

        var activity = await Service.ProjectActivity(0, 1).SingleAsync();

        activity.ChangeTypes.Should().Contain("CreateEntryChange");
    }

    [Fact]
    public async Task ProjectActivity_ChangeInfo_AddsHomographSubscriptToSubject_OnlyWhenAssigned()
    {
        await DataModel.AddChange(ClientId, new CreateEntryChange(new Entry
        {
            Id = Guid.NewGuid(),
            LexemeForm = new MultiString { ["en"] = "plain" }
        }), new CommitMetadata { AuthorName = "A", AuthorId = "a" });
        await DataModel.AddChange(ClientId, new CreateEntryChange(new Entry
        {
            Id = Guid.NewGuid(),
            LexemeForm = new MultiString { ["en"] = "homograph" },
            HomographNumber = 2
        }), new CommitMetadata { AuthorName = "A", AuthorId = "a" });

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery()).ToArrayAsync();

        activities.Should().Contain(a => a.ChangeInfo.Count == 1 && a.ChangeInfo[0].Subject == "plain");
        activities.Should().Contain(a => a.ChangeInfo.Count == 1 && a.ChangeInfo[0].Subject == "homograph₂");
    }

    [Fact]
    public async Task ProjectActivity_ChangeInfo_SkipsAudioAndUsesAnotherWritingSystem()
    {
        // The headword lives only in "seh"; the alphabetically-first writing system is audio, which Entry.Headword()
        // would surface (or report "(Unknown)") — the activity headword skips audio and falls through to a real value.
        await DataModel.AddChange(ClientId, new CreateEntryChange(new Entry
        {
            Id = Guid.NewGuid(),
            LexemeForm = new MultiString
            {
                ["de-Zxxx-x-audio"] = "recording.wav",
                ["seh"] = "nyumba"
            }
        }), new CommitMetadata { AuthorName = "A", AuthorId = "a" });

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery()).ToArrayAsync();

        activities.Should().Contain(a => a.ChangeInfo.Count == 1 && a.ChangeInfo[0].Subject == "nyumba");
    }

    [Fact]
    public async Task ProjectActivity_ChangeInfo_AppliesMorphTypeMarkers()
    {
        // Suffix morph types carry a leading "-" marker (seeded as a canonical morph type on project setup).
        await DataModel.AddChange(ClientId, new CreateEntryChange(new Entry
        {
            Id = Guid.NewGuid(),
            LexemeForm = new MultiString { ["en"] = "ness" },
            MorphType = MorphTypeKind.Suffix
        }), new CommitMetadata { AuthorName = "A", AuthorId = "a" });

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery()).ToArrayAsync();

        activities.Should().Contain(a => a.ChangeInfo.Count == 1 && a.ChangeInfo[0].Subject == "-ness");
    }

    [Fact]
    public async Task ProjectActivity_ChangeInfo_EmptyEntryHasNullSubject()
    {
        var entryId = Guid.NewGuid();
        await DataModel.AddChange(ClientId, new CreateEntryChange(new Entry { Id = entryId }),
            new CommitMetadata { AuthorName = "A", AuthorId = "a" });

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery()).ToArrayAsync();

        // No displayable headword: the subject is null (the frontend renders its placeholder, never "(Unknown)"),
        // but the change still resolves to its root entry.
        activities.Should().Contain(a => a.ChangeInfo.Count == 1
            && a.ChangeInfo[0].RootEntryId == entryId
            && a.ChangeInfo[0].Subject == null);
    }

    [Fact]
    public async Task ProjectActivity_ChangeInfo_Reorder_NamesParentEntryAndMovedSense()
    {
        var entryId = Guid.NewGuid();
        await DataModel.AddChange(ClientId, new CreateEntryChange(new Entry
        {
            Id = entryId,
            LexemeForm = new MultiString { ["en"] = "run" }
        }), new CommitMetadata { AuthorName = "A", AuthorId = "a" });
        var senseId = Guid.NewGuid();
        await DataModel.AddChange(ClientId, new CreateSenseChange(new Sense
        {
            Id = senseId,
            Gloss = new MultiString { ["en"] = "to run" }
        }, entryId), new CommitMetadata { AuthorName = "A", AuthorId = "a" });
        await DataModel.AddChange(ClientId, new SetOrderChange<Sense>(senseId, 2.0), new CommitMetadata { AuthorName = "A", AuthorId = "a" });

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery()).ToArrayAsync();

        // The reorder names the parent entry as the subject and the moved sense's gloss as the target.
        activities.Should().Contain(a => a.ChangeInfo.Count == 1 && a.ChangeInfo[0].Subject == "run" && a.ChangeInfo[0].Target == "to run");
    }

    [Fact]
    public async Task ProjectActivity_ChangeInfo_NamesAVocabSubject()
    {
        var meta = new CommitMetadata { AuthorName = "A", AuthorId = "a" };
        await DataModel.AddChange(ClientId, new CreatePartOfSpeechChange(Guid.NewGuid(), new MultiString { ["en"] = "Verb" }), meta);

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery()).ToArrayAsync();

        activities.Should().Contain(a => a.ChangeInfo.Count == 1 && a.ChangeInfo[0].Subject == "Verb");
    }

    [Fact]
    public async Task ProjectActivity_ChangeInfo_NamesTheReferencedPartOfSpeech()
    {
        var meta = new CommitMetadata { AuthorName = "A", AuthorId = "a" };
        var posId = Guid.NewGuid();
        await DataModel.AddChange(ClientId, new CreatePartOfSpeechChange(posId, new MultiString { ["en"] = "Noun" }), meta);
        var entryId = Guid.NewGuid();
        await DataModel.AddChange(ClientId, new CreateEntryChange(new Entry { Id = entryId, LexemeForm = new MultiString { ["en"] = "run" } }), meta);
        var senseId = Guid.NewGuid();
        await DataModel.AddChange(ClientId, new CreateSenseChange(new Sense { Id = senseId, Gloss = new MultiString { ["en"] = "to run" } }, entryId), meta);
        await DataModel.AddChange(ClientId, new SetPartOfSpeechChange(senseId, posId), meta);

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery()).ToArrayAsync();

        // The set-part-of-speech change names the sense as its subject and the assigned part of speech as its target.
        activities.Should().Contain(a => a.ChangeInfo.Count == 1
            && a.ChangeInfo[0].Subject == "run › to run"
            && a.ChangeInfo[0].Target == "Noun");
    }

    [Fact]
    public async Task ProjectActivity_ChangeInfo_SenseWithEmptyGloss_UsesSenseNumber()
    {
        var entryId = await CreateEntry("run");
        await CreateSense(entryId, gloss: null, order: 1.0);

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery()).ToArrayAsync();

        // Create-sense reads as an entry-level change ("run · Added sense senseN"): the subject is the
        // parent entry headword and the sense identifier goes to Target (subscript for an empty gloss).
        activities.Should().Contain(a => a.ChangeInfo.Count == 1 && a.ChangeInfo[0].Subject == "run" && a.ChangeInfo[0].Target == "sense₁");
    }

    [Fact]
    public async Task ProjectActivity_ChangeInfo_DuplicateGlosses_AppendSenseNumber()
    {
        var entryId = await CreateEntry("bat");
        await CreateSense(entryId, gloss: "flying mammal", order: 1.0);
        await CreateSense(entryId, gloss: "flying mammal", order: 2.0);

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery()).ToArrayAsync();

        // Both senses share a gloss, so each Target is disambiguated by its 1-based position as a subscript
        // (matches the homograph-number convention so the number reads as a disambiguator, not a value).
        // Subject is the parent entry headword since these are create-sense summaries.
        activities.Should().Contain(a => a.ChangeInfo.Count == 1 && a.ChangeInfo[0].Subject == "bat" && a.ChangeInfo[0].Target == "flying mammal₁");
        activities.Should().Contain(a => a.ChangeInfo.Count == 1 && a.ChangeInfo[0].Subject == "bat" && a.ChangeInfo[0].Target == "flying mammal₂");
    }

    [Fact]
    public async Task ProjectActivity_ChangeInfo_UniqueGloss_HasNoSenseNumber()
    {
        var entryId = await CreateEntry("run");
        await CreateSense(entryId, gloss: "to run", order: 1.0);
        await CreateSense(entryId, gloss: "a jog", order: 2.0);

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery()).ToArrayAsync();

        // Distinct glosses are unambiguous, so no number is appended. Create-sense subject is the parent
        // entry headword; the sense gloss goes to Target ("run · Added sense to run").
        activities.Should().Contain(a => a.ChangeInfo.Count == 1 && a.ChangeInfo[0].Subject == "run" && a.ChangeInfo[0].Target == "to run");
        activities.Should().Contain(a => a.ChangeInfo.Count == 1 && a.ChangeInfo[0].Subject == "run" && a.ChangeInfo[0].Target == "a jog");
    }

    [Fact]
    public async Task ProjectActivity_ChangeInfo_NamesSemanticDomainSubject()
    {
        var meta = Meta();
        await DataModel.AddChange(ClientId, new CreateSemanticDomainChange(Guid.NewGuid(), new MultiString { ["en"] = "Food" }, "5.2"), meta);

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery()).ToArrayAsync();

        activities.Should().Contain(a => a.ChangeInfo.Count == 1 && a.ChangeInfo[0].Subject == "5.2 Food");
    }

    [Fact]
    public async Task ProjectActivity_ChangeInfo_NamesPublicationSubject()
    {
        await AddNewPublicationCommit(Meta(), "Main Dictionary");

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery()).ToArrayAsync();

        activities.Should().Contain(a => a.ChangeInfo.Count == 1 && a.ChangeInfo[0].Subject == "Main Dictionary");
    }

    [Fact]
    public async Task ProjectActivity_ChangeInfo_NamesComplexFormTypeSubject()
    {
        await DataModel.AddChange(ClientId, new CreateComplexFormType(Guid.NewGuid(), new MultiString { ["en"] = "Compound" }), Meta());

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery()).ToArrayAsync();

        activities.Should().Contain(a => a.ChangeInfo.Count == 1 && a.ChangeInfo[0].Subject == "Compound");
    }

    [Fact]
    public async Task ProjectActivity_ChangeInfo_NamesMorphTypeSubject()
    {
        // Morph types are seeded on project setup; patch a canonical one so the change resolves through the MorphType bucket.
        var suffixId = CanonicalMorphTypes.All[MorphTypeKind.Suffix].Id;
        var patch = new JsonPatchDocument<MorphType>();
        patch.Replace(m => m.SecondaryOrder, 99);
        await DataModel.AddChange(ClientId, new JsonPatchChange<MorphType>(suffixId, patch), Meta());

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery()).ToArrayAsync();

        activities.Should().Contain(a => a.ChangeInfo.Count == 1 && a.ChangeInfo[0].Subject == "suffix");
    }

    [Fact]
    public async Task ProjectActivity_ChangeInfo_NamesExampleSentenceSubject()
    {
        var entryId = await CreateEntry("run");
        var senseId = await CreateSense(entryId, gloss: "to run", order: 1.0);
        await DataModel.AddChange(ClientId, new CreateExampleSentenceChange(new ExampleSentence
        {
            Id = Guid.NewGuid(),
            Sentence = new() { ["en"] = new RichString("I run daily") }
        }, senseId), Meta());

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery()).ToArrayAsync();

        // An example resolves to its parent sense's subject and its root entry.
        activities.Should().Contain(a => a.ChangeInfo.Count == 1
            && a.ChangeInfo[0].Subject == "run › to run"
            && a.ChangeInfo[0].RootEntryId == entryId);
    }

    [Fact]
    public async Task ProjectActivity_ChangeInfo_NamesComplexFormComponentSubject_AndComponentTarget()
    {
        var complexFormId = await CreateEntry("blackbird");
        var componentId = await CreateEntry("bird");
        await AddComponent(complexFormId, componentId);

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery()).ToArrayAsync();

        // The link's subject is the complex form; the target is the component being linked.
        activities.Should().Contain(a => a.ChangeInfo.Count == 1
            && a.ChangeInfo[0].Subject == "blackbird"
            && a.ChangeInfo[0].RootEntryId == complexFormId
            && a.ChangeInfo[0].Target == "bird");
    }

    [Fact]
    public async Task ProjectActivity_ChangeInfo_SetComplexFormComponent_NamesComplexFormAndNewComponent()
    {
        var complexFormId = await CreateEntry("blackbird");
        var componentId = await CreateEntry("black");
        var newComponentId = await CreateEntry("bird");
        var component = await AddComponent(complexFormId, componentId);
        await DataModel.AddChange(ClientId, SetComplexFormComponentChange.NewComponent(component.Id, newComponentId), Meta());

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery()).ToArrayAsync();

        activities.Should().Contain(a => a.ChangeInfo.Count == 1
            && a.ChangeInfo[0].Subject == "blackbird"
            && a.ChangeInfo[0].Target == "bird");
    }

    [Fact]
    public async Task ProjectActivity_ChangeInfo_RemoveSemanticDomain_NamesRemovedDomain()
    {
        var entryId = await CreateEntry("run");
        var domainId = Guid.NewGuid();
        await DataModel.AddChange(ClientId, new CreateSemanticDomainChange(domainId, new MultiString { ["en"] = "Food" }, "5.2"), Meta());
        var domain = await DataModel.GetLatest<SemanticDomain>(domainId);
        var senseId = await CreateSense(entryId, gloss: "to run", order: 1.0, semanticDomains: [domain!]);
        await DataModel.AddChange(ClientId, new RemoveSemanticDomainChange(domainId, senseId), Meta());

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery()).ToArrayAsync();

        // The remove change names the sense as subject and the removed domain as target.
        activities.Should().Contain(a => a.ChangeInfo.Count == 1
            && a.ChangeInfo[0].Subject == "run › to run"
            && a.ChangeInfo[0].Target == "5.2 Food");
    }

    [Fact]
    public async Task ProjectActivity_ChangeInfo_RemovePublication_NamesRemovedPublication()
    {
        var entryId = await CreateEntry("run");
        var publicationId = Guid.NewGuid();
        await DataModel.AddChange(ClientId, new CreatePublicationChange(publicationId, new MultiString { ["en"] = "Main Dictionary" }), Meta());
        var publication = await DataModel.GetLatest<Publication>(publicationId);
        await DataModel.AddChange(ClientId, new AddPublicationChange(entryId, publication!), Meta());
        await DataModel.AddChange(ClientId, new RemovePublicationChange(entryId, publicationId), Meta());

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery()).ToArrayAsync();

        // The remove change names the entry as subject and the removed publication as target.
        activities.Should().Contain(a => a.ChangeInfo.Count == 1
            && a.ChangeInfo[0].Subject == "run"
            && a.ChangeInfo[0].Target == "Main Dictionary");
    }

    [Fact]
    public async Task ProjectActivity_ChangeInfo_RemoveComplexFormType_NamesRemovedType()
    {
        var entryId = await CreateEntry("blackbird");
        var typeId = Guid.NewGuid();
        await DataModel.AddChange(ClientId, new CreateComplexFormType(typeId, new MultiString { ["en"] = "Compound" }), Meta());
        var type = await DataModel.GetLatest<ComplexFormType>(typeId);
        await DataModel.AddChange(ClientId, new AddComplexFormTypeChange(entryId, type!), Meta());
        await DataModel.AddChange(ClientId, new RemoveComplexFormTypeChange(entryId, typeId), Meta());

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery()).ToArrayAsync();

        activities.Should().Contain(a => a.ChangeInfo.Count == 1
            && a.ChangeInfo[0].Subject == "blackbird"
            && a.ChangeInfo[0].Target == "Compound");
    }

    [Fact]
    public async Task ProjectActivity_ChangeInfo_Reorder_NamesParentSenseAndMovedExample()
    {
        var entryId = await CreateEntry("run");
        var senseId = await CreateSense(entryId, gloss: "to run", order: 1.0);
        var exampleId = Guid.NewGuid();
        await DataModel.AddChange(ClientId, new CreateExampleSentenceChange(new ExampleSentence
        {
            Id = exampleId,
            Sentence = new() { ["en"] = new RichString("I run") }
        }, senseId), Meta());
        await DataModel.AddChange(ClientId, new SetOrderChange<ExampleSentence>(exampleId, 2.0), Meta());

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery()).ToArrayAsync();

        // Reordering an example names its parent sense as the subject and the example's root entry.
        activities.Should().Contain(a => a.ChangeInfo.Count == 1
            && a.ChangeInfo[0].Subject == "run › to run"
            && a.ChangeInfo[0].RootEntryId == entryId);
    }

    [Fact]
    public async Task ProjectActivity_ChangeInfo_Reorder_NamesComplexFormAndMovedComponent()
    {
        var complexFormId = await CreateEntry("blackbird");
        var componentId = await CreateEntry("bird");
        var component = await AddComponent(complexFormId, componentId);
        await DataModel.AddChange(ClientId, new SetOrderChange<ComplexFormComponent>(component.Id, 2.0), Meta());

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery()).ToArrayAsync();

        // Reordering a component names the complex form as subject and the moved component as target.
        activities.Should().Contain(a => a.ChangeInfo.Count == 1
            && a.ChangeInfo[0].Subject == "blackbird"
            && a.ChangeInfo[0].RootEntryId == complexFormId
            && a.ChangeInfo[0].Target == "bird");
    }

    private static CommitMetadata Meta() => new() { AuthorName = "A", AuthorId = "a" };

    private async Task<Guid> CreateEntry(string headword)
    {
        var entryId = Guid.NewGuid();
        await DataModel.AddChange(ClientId, new CreateEntryChange(new Entry
        {
            Id = entryId,
            LexemeForm = new MultiString { ["en"] = headword }
        }), Meta());
        return entryId;
    }

    private async Task<Guid> CreateSense(Guid entryId, string? gloss, double order, IList<SemanticDomain>? semanticDomains = null)
    {
        var senseId = Guid.NewGuid();
        var sense = new Sense
        {
            Id = senseId,
            Order = order,
            Gloss = gloss is null ? new MultiString() : new MultiString { ["en"] = gloss },
            SemanticDomains = semanticDomains ?? []
        };
        await DataModel.AddChange(ClientId, new CreateSenseChange(sense, entryId), Meta());
        return senseId;
    }

    private async Task<ComplexFormComponent> AddComponent(Guid complexFormId, Guid componentId)
    {
        var component = ComplexFormComponent.FromEntries(
            new Entry { Id = complexFormId },
            new Entry { Id = componentId });
        await DataModel.AddChange(ClientId, new AddEntryComponentChange(component), Meta());
        return component;
    }

    private async Task<Commit> AddEntryCommit(CommitMetadata metadata, string? headword = null)
    {
        var entry = headword is null
            ? await AutoFaker.EntryReadyForCreation(_fixture.Api)
            : new Entry { Id = Guid.NewGuid(), LexemeForm = new MultiString { ["en"] = headword } };
        return await DataModel.AddChange(ClientId, new CreateEntryChange(entry), metadata);
    }

    private async Task<Commit> AddNewPublicationCommit(CommitMetadata metadata, string publicationName = "Test Publication")
    {
        return await DataModel.AddChange(ClientId, new CreatePublicationChange(Guid.NewGuid(), new MultiString
        {
            ["en"] = publicationName
        }), metadata);
    }

    private async Task<Commit> AddNewPartOfSpeechCommit(CommitMetadata metadata, string partOfSpeechName = "Test Part of Speech")
    {
        return await DataModel.AddChange(ClientId, new CreatePartOfSpeechChange(Guid.NewGuid(), new MultiString
        {
            ["en"] = partOfSpeechName
        }), metadata);
    }

    private async Task SetSyncDate(Guid commitId, DateTimeOffset syncDate)
    {
        var db = _fixture.DbContext;
        var commit = await db.Set<Commit>().SingleAsync(c => c.Id == commitId);
        commit.SetSyncDate(syncDate);
        db.Entry(commit).Property(c => c.Metadata).IsModified = true;
        await db.SaveChangesAsync();
    }
}
