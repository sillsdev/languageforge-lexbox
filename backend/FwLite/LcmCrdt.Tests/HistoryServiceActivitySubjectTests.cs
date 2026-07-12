using LcmCrdt.Changes;
using LcmCrdt.Changes.Entries;
using LcmCrdt.Utils;
using SIL.Harmony.Core;
using SystemTextJsonPatch;

namespace LcmCrdt.Tests;

// Lower-importance coverage: how a change names the entity it's about (subject/target labels, homograph and
// sense numbering, deleted-object recovery). Split from the core ProjectActivity paging/filtering/sorting tests.
public class HistoryServiceActivitySubjectTests : HistoryServiceActivityTestsBase
{
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

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery());

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

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery());

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

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery());

        activities.Should().Contain(a => a.ChangeInfo.Count == 1 && a.ChangeInfo[0].Subject == "-ness");
    }

    [Fact]
    public async Task ProjectActivity_ChangeInfo_EmptyEntryHasNullSubject()
    {
        var entryId = Guid.NewGuid();
        await DataModel.AddChange(ClientId, new CreateEntryChange(new Entry { Id = entryId }),
            new CommitMetadata { AuthorName = "A", AuthorId = "a" });

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery());

        // No displayable headword: the subject and root-entry headword are null (the frontend renders its
        // placeholder, never "(Unknown)"), but the change still resolves to its root entry.
        activities.Should().Contain(a => a.ChangeInfo.Count == 1
            && a.ChangeInfo[0].RootEntryId == entryId
            && a.ChangeInfo[0].Subject == null
            && a.ChangeInfo[0].RootEntryHeadword == null);
    }

    [Fact]
    public async Task ProjectActivity_ChangeInfo_UnlabellableSenseHasNullSubject()
    {
        var entryId = Guid.NewGuid();
        await DataModel.AddChange(ClientId, new CreateEntryChange(new Entry { Id = entryId }), Meta());
        var senseId = await CreateSense(entryId, gloss: null, order: 1.0);
        var patch = new JsonPatchDocument<Sense>();
        patch.Replace(s => s.Gloss["en"], "");
        await DataModel.AddChange(ClientId, new JsonPatchChange<Sense>(senseId, patch), Meta());

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery());

        // Neither a headword nor a gloss to label the sense with: the subject is null (the frontend just
        // omits the leading subject token), never a hardcoded English placeholder.
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

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery());

        // The reorder names the parent entry as the subject and the moved sense's gloss as the target.
        activities.Should().Contain(a => a.ChangeInfo.Count == 1 && a.ChangeInfo[0].Subject == "run" && a.ChangeInfo[0].Target == "to run");
    }

    [Fact]
    public async Task ProjectActivity_ChangeInfo_NamesAVocabSubject()
    {
        var meta = new CommitMetadata { AuthorName = "A", AuthorId = "a" };
        await DataModel.AddChange(ClientId, new CreatePartOfSpeechChange(Guid.NewGuid(), new MultiString { ["en"] = "Verb" }), meta);

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery());

        // A vocab object has no root entry, so no root-entry headword either.
        activities.Should().Contain(a => a.ChangeInfo.Count == 1
            && a.ChangeInfo[0].Subject == "Verb"
            && a.ChangeInfo[0].RootEntryId == null
            && a.ChangeInfo[0].RootEntryHeadword == null);
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

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery());

        // The set-part-of-speech change names the sense as its subject and the assigned part of speech as its target.
        activities.Should().Contain(a => a.ChangeInfo.Count == 1
            && a.ChangeInfo[0].Subject == "run › to run"
            && a.ChangeInfo[0].Target == "Noun");
    }

    [Fact]
    public async Task ProjectActivity_ChangeInfo_LoneSense_HasNoSenseNumber()
    {
        var entryId = await CreateEntry("run");
        await CreateSense(entryId, gloss: "to run", order: 1.0);

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery());

        // A lone sense needs no number (nothing to disambiguate): "run · Added sense to run".
        activities.Should().Contain(a => a.ChangeInfo.Count == 1 && a.ChangeInfo[0].Subject == "run" && a.ChangeInfo[0].Target == "to run");
    }

    [Fact]
    public async Task ProjectActivity_ChangeInfo_MultipleSenses_NumberedPositionally()
    {
        var entryId = await CreateEntry("run");
        await CreateSense(entryId, gloss: "to run", order: 1.0);
        await CreateSense(entryId, gloss: "a jog", order: 2.0);
        await CreateSense(entryId, gloss: null, order: 3.0);

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery());

        // With more than one sense every sense is numbered by position (like FieldWorks), independent of the
        // gloss — unique or not — shown as a subscript after the gloss, like a homograph number. An empty
        // gloss shows the parenthesized position instead.
        activities.Should().Contain(a => a.ChangeInfo.Count == 1 && a.ChangeInfo[0].Subject == "run" && a.ChangeInfo[0].Target == "to run₁");
        activities.Should().Contain(a => a.ChangeInfo.Count == 1 && a.ChangeInfo[0].Subject == "run" && a.ChangeInfo[0].Target == "a jog₂");
        activities.Should().Contain(a => a.ChangeInfo.Count == 1 && a.ChangeInfo[0].Subject == "run" && a.ChangeInfo[0].Target == "(3)");
    }

    [Fact]
    public async Task ProjectActivity_ChangeInfo_DeletedEntry_StillHasHeadword()
    {
        var entryId = await CreateEntry("run");
        await DataModel.AddChange(ClientId, new SIL.Harmony.Changes.DeleteChange<Entry>(entryId), Meta());

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery());

        // The deleted entry is gone from the projected tables; its label is recovered from its last snapshot.
        activities.Should().Contain(a => a.ChangeTypes.Contains("delete:Entry")
            && a.ChangeInfo[0].Subject == "run"
            && a.ChangeInfo[0].RootEntryHeadword == "run");
    }

    [Fact]
    public async Task ProjectActivity_ChangeInfo_DeletedSense_StillHasLabel_WithoutPosition()
    {
        var entryId = await CreateEntry("run");
        var senseId = await CreateSense(entryId, gloss: "to run", order: 1.0);
        await DataModel.AddChange(ClientId, new SIL.Harmony.Changes.DeleteChange<Sense>(senseId), Meta());

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery());

        // Recovered from its snapshot; absent from the live sibling list, so labeled by gloss with no number.
        activities.Should().Contain(a => a.ChangeTypes.Contains("delete:Sense")
            && a.ChangeInfo[0].Subject == "run › to run");
    }

    [Fact]
    public async Task ProjectActivity_ChangeInfo_NamesSemanticDomainSubject()
    {
        var meta = Meta();
        await DataModel.AddChange(ClientId, new CreateSemanticDomainChange(Guid.NewGuid(), new MultiString { ["en"] = "Food" }, "5.2"), meta);

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery());

        activities.Should().Contain(a => a.ChangeInfo.Count == 1 && a.ChangeInfo[0].Subject == "5.2 Food");
    }

    [Fact]
    public async Task ProjectActivity_ChangeInfo_NamesPublicationSubject()
    {
        await AddNewPublicationCommit(Meta(), "Main Dictionary");

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery());

        activities.Should().Contain(a => a.ChangeInfo.Count == 1 && a.ChangeInfo[0].Subject == "Main Dictionary");
    }

    [Fact]
    public async Task ProjectActivity_ChangeInfo_NamesComplexFormTypeSubject()
    {
        await DataModel.AddChange(ClientId, new CreateComplexFormType(Guid.NewGuid(), new MultiString { ["en"] = "Compound" }), Meta());

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery());

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

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery());

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

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery());

        // An example resolves to its parent sense's subject and its root entry (id + headword).
        activities.Should().Contain(a => a.ChangeInfo.Count == 1
            && a.ChangeInfo[0].Subject == "run › to run"
            && a.ChangeInfo[0].RootEntryId == entryId
            && a.ChangeInfo[0].RootEntryHeadword == "run");
    }

    [Fact]
    public async Task ProjectActivity_ChangeInfo_NamesComplexFormComponentSubject_AndComponentTarget()
    {
        var complexFormId = await CreateEntry("blackbird");
        var componentId = await CreateEntry("bird");
        await AddComponent(complexFormId, componentId);

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery());

        // The link's subject is the complex form; the target is the component being linked.
        activities.Should().Contain(a => a.ChangeInfo.Count == 1
            && a.ChangeInfo[0].Subject == "blackbird"
            && a.ChangeInfo[0].RootEntryId == complexFormId
            && a.ChangeInfo[0].Target == "bird");
    }

    [Fact]
    public async Task ProjectActivity_ChangeInfo_RemovedComponent_NamesEndpointsFromCreateChange()
    {
        var complexFormId = await CreateEntry("blackbird");
        var componentId = await CreateEntry("bird");
        var component = await AddComponent(complexFormId, componentId);
        // Delete the link: it leaves the projection, so its endpoints must come from the create change.
        await DataModel.AddChange(ClientId, new SIL.Harmony.Changes.DeleteChange<ComplexFormComponent>(component.Id), Meta());

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery());

        // The delete still names the complex form (subject) and component (target) for orientation.
        activities.Should().Contain(a => a.ChangeInfo.Count == 1
            && a.ChangeInfo[0].Subject == "blackbird"
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

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery());

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

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery());

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

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery());

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

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery());

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

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery());

        // Reordering an example names its parent sense as the subject and the example's root entry (id + headword).
        activities.Should().Contain(a => a.ChangeInfo.Count == 1
            && a.ChangeInfo[0].Subject == "run › to run"
            && a.ChangeInfo[0].RootEntryId == entryId
            && a.ChangeInfo[0].RootEntryHeadword == "run");
    }

    [Fact]
    public async Task ProjectActivity_ChangeInfo_Reorder_NamesComplexFormAndMovedComponent()
    {
        var complexFormId = await CreateEntry("blackbird");
        var componentId = await CreateEntry("bird");
        var component = await AddComponent(complexFormId, componentId);
        await DataModel.AddChange(ClientId, new SetOrderChange<ComplexFormComponent>(component.Id, 2.0), Meta());

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery());

        // Reordering a component names the complex form as subject and the moved component as target.
        activities.Should().Contain(a => a.ChangeInfo.Count == 1
            && a.ChangeInfo[0].Subject == "blackbird"
            && a.ChangeInfo[0].RootEntryId == complexFormId
            && a.ChangeInfo[0].Target == "bird");
    }
}
