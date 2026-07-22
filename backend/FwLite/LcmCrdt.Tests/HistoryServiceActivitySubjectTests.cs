using LcmCrdt.Changes;
using LcmCrdt.Changes.Entries;
using SIL.Harmony.Core;
using SystemTextJsonPatch;

namespace LcmCrdt.Tests;

public class HistoryServiceActivitySubjectTests : HistoryServiceActivityTestsBase
{
    // --- Subject/label formatting: how a headword or gloss is rendered into a subject/target string. ---

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
        // gloss shows a dotted-circle placeholder for the subscript to attach to.
        activities.Should().Contain(a => a.ChangeInfo.Count == 1 && a.ChangeInfo[0].Subject == "run" && a.ChangeInfo[0].Target == "to run₁");
        activities.Should().Contain(a => a.ChangeInfo.Count == 1 && a.ChangeInfo[0].Subject == "run" && a.ChangeInfo[0].Target == "a jog₂");
        activities.Should().Contain(a => a.ChangeInfo.Count == 1 && a.ChangeInfo[0].Subject == "run" && a.ChangeInfo[0].Target == "◌₃");
    }

    [Fact]
    public async Task ProjectActivity_ChangeInfo_EmptyEntryHasNullSubject()
    {
        var entryId = Guid.NewGuid();
        await DataModel.AddChange(ClientId, new CreateEntryChange(new Entry { Id = entryId }),
            new CommitMetadata { AuthorName = "A", AuthorId = "a" });

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery());

        // No displayable headword: the subject and owning-entry headword are null (the frontend renders its
        // placeholder, never "(Unknown)"), but the change still resolves to its owning entry.
        activities.Should().Contain(a => a.ChangeInfo.Count == 1
            && a.ChangeInfo[0].OwningEntryId == entryId
            && a.ChangeInfo[0].Subject == null
            && a.ChangeInfo[0].OwningEntryHeadword == null);
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
            && a.ChangeInfo[0].OwningEntryId == entryId
            && a.ChangeInfo[0].Subject == null);
    }

    // --- Possibilities, writing systems, and custom views name themselves by their display name, and recover
    // that label after deletion. ---

    [Fact]
    public async Task ProjectActivity_ChangeInfo_NamesPartOfSpeechSubject_EvenAfterDelete()
    {
        var id = Guid.NewGuid();
        await DataModel.AddChange(ClientId, new CreatePartOfSpeechChange(id, new MultiString { ["en"] = "Verb" }), Meta());

        var live = await Service.ProjectActivity(0, 100, new ActivityQuery());
        // A possibility names itself by its display name and has no owning entry (so no owning-entry headword either).
        live.Should().Contain(a => a.ChangeInfo.Count == 1
            && a.ChangeInfo[0].Subject == "Verb"
            && a.ChangeInfo[0].OwningEntryId == null
            && a.ChangeInfo[0].OwningEntryHeadword == null);

        await DataModel.AddChange(ClientId, new SIL.Harmony.Changes.DeleteChange<PartOfSpeech>(id), Meta());

        var afterDelete = await Service.ProjectActivity(0, 100, new ActivityQuery());
        // The delete drops it from the projection; its label is recovered from the last snapshot.
        afterDelete.Should().Contain(a => a.ChangeTypes.Contains("delete:PartOfSpeech")
            && a.ChangeInfo[0].Subject == "Verb");
    }

    [Fact]
    public async Task ProjectActivity_ChangeInfo_NamesSemanticDomainSubject_EvenAfterDelete()
    {
        var id = Guid.NewGuid();
        await DataModel.AddChange(ClientId, new CreateSemanticDomainChange(id, new MultiString { ["en"] = "Food" }, "5.2"), Meta());

        var live = await Service.ProjectActivity(0, 100, new ActivityQuery());
        // A domain shows as "code name".
        live.Should().Contain(a => a.ChangeInfo.Count == 1
            && a.ChangeInfo[0].Subject == "5.2 Food"
            && a.ChangeInfo[0].OwningEntryId == null);

        await DataModel.AddChange(ClientId, new SIL.Harmony.Changes.DeleteChange<SemanticDomain>(id), Meta());

        var afterDelete = await Service.ProjectActivity(0, 100, new ActivityQuery());
        afterDelete.Should().Contain(a => a.ChangeTypes.Contains("delete:SemanticDomain")
            && a.ChangeInfo[0].Subject == "5.2 Food");
    }

    [Fact]
    public async Task ProjectActivity_ChangeInfo_NamesPublicationSubject_EvenAfterDelete()
    {
        var id = Guid.NewGuid();
        await DataModel.AddChange(ClientId, new CreatePublicationChange(id, new MultiString { ["en"] = "Main Dictionary" }), Meta());

        var live = await Service.ProjectActivity(0, 100, new ActivityQuery());
        live.Should().Contain(a => a.ChangeInfo.Count == 1
            && a.ChangeInfo[0].Subject == "Main Dictionary"
            && a.ChangeInfo[0].OwningEntryId == null);

        await DataModel.AddChange(ClientId, new SIL.Harmony.Changes.DeleteChange<Publication>(id), Meta());

        var afterDelete = await Service.ProjectActivity(0, 100, new ActivityQuery());
        afterDelete.Should().Contain(a => a.ChangeTypes.Contains("delete:Publication")
            && a.ChangeInfo[0].Subject == "Main Dictionary");
    }

    [Fact]
    public async Task ProjectActivity_ChangeInfo_NamesComplexFormTypeSubject_EvenAfterDelete()
    {
        var id = Guid.NewGuid();
        await DataModel.AddChange(ClientId, new CreateComplexFormType(id, new MultiString { ["en"] = "Compound" }), Meta());

        var live = await Service.ProjectActivity(0, 100, new ActivityQuery());
        live.Should().Contain(a => a.ChangeInfo.Count == 1
            && a.ChangeInfo[0].Subject == "Compound"
            && a.ChangeInfo[0].OwningEntryId == null);

        await DataModel.AddChange(ClientId, new SIL.Harmony.Changes.DeleteChange<ComplexFormType>(id), Meta());

        var afterDelete = await Service.ProjectActivity(0, 100, new ActivityQuery());
        afterDelete.Should().Contain(a => a.ChangeTypes.Contains("delete:ComplexFormType")
            && a.ChangeInfo[0].Subject == "Compound");
    }

    [Fact]
    public async Task ProjectActivity_ChangeInfo_NamesWritingSystemSubject_EvenAfterDelete()
    {
        var id = Guid.NewGuid();
        await DataModel.AddChange(ClientId, new CreateWritingSystemChange(new WritingSystem
        {
            Id = id,
            WsId = "fr",
            Name = "French",
            Abbreviation = "fr",
            Font = "Arial",
            Type = WritingSystemType.Vernacular
        }, id, 0), Meta());

        var live = await Service.ProjectActivity(0, 100, new ActivityQuery());
        // A writing system names itself by its plain display name and has no owning entry.
        live.Should().Contain(a => a.ChangeInfo.Count == 1
            && a.ChangeInfo[0].Subject == "French"
            && a.ChangeInfo[0].OwningEntryId == null);

        await DataModel.AddChange(ClientId, new SIL.Harmony.Changes.DeleteChange<WritingSystem>(id), Meta());

        var afterDelete = await Service.ProjectActivity(0, 100, new ActivityQuery());
        // The delete drops it from the projection; its name is recovered from the last snapshot.
        afterDelete.Should().Contain(a => a.ChangeTypes.Contains("delete:WritingSystem")
            && a.ChangeInfo[0].Subject == "French");
    }

    [Fact]
    public async Task ProjectActivity_ChangeInfo_NamesCustomViewSubject_EvenAfterDelete()
    {
        var id = Guid.NewGuid();
        await DataModel.AddChange(ClientId, new CreateCustomViewChange(id, new CustomView
        {
            Id = id,
            Name = "My View",
            Base = ViewBase.FwLite
        }), Meta());

        var live = await Service.ProjectActivity(0, 100, new ActivityQuery());
        live.Should().Contain(a => a.ChangeInfo.Count == 1
            && a.ChangeInfo[0].Subject == "My View"
            && a.ChangeInfo[0].OwningEntryId == null);

        await DataModel.AddChange(ClientId, new SIL.Harmony.Changes.DeleteChange<CustomView>(id), Meta());

        var afterDelete = await Service.ProjectActivity(0, 100, new ActivityQuery());
        afterDelete.Should().Contain(a => a.ChangeTypes.Contains("delete:CustomView")
            && a.ChangeInfo[0].Subject == "My View");
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

    // --- Deleted entries and senses recover their labels from the last snapshot. ---

    [Fact]
    public async Task ProjectActivity_ChangeInfo_DeletedEntry_StillHasHeadword()
    {
        var entryId = await CreateEntry("run");
        await DataModel.AddChange(ClientId, new SIL.Harmony.Changes.DeleteChange<Entry>(entryId), Meta());

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery());

        // The deleted entry is gone from the projected tables; its label is recovered from its last snapshot.
        activities.Should().Contain(a => a.ChangeTypes.Contains("delete:Entry")
            && a.ChangeInfo[0].Subject == "run"
            && a.ChangeInfo[0].OwningEntryHeadword == "run");
    }

    [Fact]
    public async Task ProjectActivity_ChangeInfo_DeletedSense_StillHasLabel_WithoutPosition()
    {
        var entryId = await CreateEntry("run");
        var senseId = await CreateSense(entryId, gloss: "to run", order: 1.0);
        await DataModel.AddChange(ClientId, new SIL.Harmony.Changes.DeleteChange<Sense>(senseId), Meta());

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery());

        // Removing a sense mirrors adding one: the entry is the subject, the removed sense the target.
        // Recovered from its snapshot; absent from the live sibling list, so the target gloss carries no number.
        activities.Should().Contain(a => a.ChangeTypes.Contains("delete:Sense")
            && a.ChangeInfo[0].Subject == "run"
            && a.ChangeInfo[0].Target == "to run");
    }

    // --- Reference changes name the entity holding the reference (subject) and the referenced object (target),
    // grouped by referenced type. Add and Remove of the same reference name the same subject/target, so an
    // "Added"/"Removed" pair reads as inverses in the activity feed. ---

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
    public async Task ProjectActivity_ChangeInfo_AddSemanticDomain_NamesAddedDomain()
    {
        var entryId = await CreateEntry("run");
        var senseId = await CreateSense(entryId, gloss: "to run", order: 1.0);
        var domainId = Guid.NewGuid();
        await DataModel.AddChange(ClientId, new CreateSemanticDomainChange(domainId, new MultiString { ["en"] = "Food" }, "5.2"), Meta());
        var domain = await DataModel.GetLatest<SemanticDomain>(domainId);
        await DataModel.AddChange(ClientId, new AddSemanticDomainChange(domain!, senseId), Meta());

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery());

        activities.Should().Contain(a => a.ChangeTypes.Contains("AddSemanticDomainChange")
            && a.ChangeInfo[0].Subject == "run › to run"
            && a.ChangeInfo[0].Target == "5.2 Food");
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
    public async Task ProjectActivity_ChangeInfo_AddPublication_NamesAddedPublication()
    {
        var entryId = await CreateEntry("run");
        var publicationId = Guid.NewGuid();
        await DataModel.AddChange(ClientId, new CreatePublicationChange(publicationId, new MultiString { ["en"] = "Main Dictionary" }), Meta());
        var publication = await DataModel.GetLatest<Publication>(publicationId);
        await DataModel.AddChange(ClientId, new AddPublicationChange(entryId, publication!), Meta());

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery());

        activities.Should().Contain(a => a.ChangeTypes.Contains("AddPublicationChange")
            && a.ChangeInfo[0].Subject == "run"
            && a.ChangeInfo[0].Target == "Main Dictionary");
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
    public async Task ProjectActivity_ChangeInfo_AddComplexFormType_NamesAddedType()
    {
        var entryId = await CreateEntry("blackbird");
        var typeId = Guid.NewGuid();
        await DataModel.AddChange(ClientId, new CreateComplexFormType(typeId, new MultiString { ["en"] = "Compound" }), Meta());
        var type = await DataModel.GetLatest<ComplexFormType>(typeId);
        await DataModel.AddChange(ClientId, new AddComplexFormTypeChange(entryId, type!), Meta());

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery());

        activities.Should().Contain(a => a.ChangeTypes.Contains("AddComplexFormTypeChange")
            && a.ChangeInfo[0].Subject == "blackbird"
            && a.ChangeInfo[0].Target == "Compound");
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
    public async Task ProjectActivity_ChangeInfo_NamesComplexFormComponentSubject_AndComponentTarget()
    {
        var complexFormId = await CreateEntry("blackbird");
        var componentId = await CreateEntry("bird");
        await AddComponent(complexFormId, componentId);

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery());

        // The link's subject is the complex form; the target is the component being linked.
        activities.Should().Contain(a => a.ChangeInfo.Count == 1
            && a.ChangeInfo[0].Subject == "blackbird"
            && a.ChangeInfo[0].OwningEntryId == complexFormId
            && a.ChangeInfo[0].Target == "bird");
    }

    [Fact]
    public async Task ProjectActivity_ChangeInfo_RemovedComponent_StillNamesEndpoints()
    {
        var complexFormId = await CreateEntry("blackbird");
        var componentId = await CreateEntry("bird");
        var component = await AddComponent(complexFormId, componentId);
        // Delete the link: it leaves the projection, so its endpoints must be recovered from its last snapshot.
        await DataModel.AddChange(ClientId, new SIL.Harmony.Changes.DeleteChange<ComplexFormComponent>(component.Id), Meta());

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery());

        // The delete still names the complex form (subject) and component (target) for orientation.
        activities.Should().Contain(a => a.ChangeInfo.Count == 1
            && a.ChangeInfo[0].Subject == "blackbird"
            && a.ChangeInfo[0].Target == "bird");
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

        // An example resolves to its parent sense's subject and its owning entry (id + headword); the target is a
        // short snippet of the sentence text (here short enough to show whole).
        activities.Should().Contain(a => a.ChangeInfo.Count == 1
            && a.ChangeInfo[0].Subject == "run › to run"
            && a.ChangeInfo[0].OwningEntryId == entryId
            && a.ChangeInfo[0].OwningEntryHeadword == "run"
            && a.ChangeInfo[0].Target == "I run daily");
    }

    [Fact]
    public async Task ProjectActivity_ChangeInfo_DeletedExample_StillHasSentenceSnippet()
    {
        var entryId = await CreateEntry("run");
        var senseId = await CreateSense(entryId, gloss: "to run", order: 1.0);
        var exampleId = Guid.NewGuid();
        await DataModel.AddChange(ClientId, new CreateExampleSentenceChange(new ExampleSentence
        {
            Id = exampleId,
            Sentence = new() { ["en"] = new RichString("I run daily") }
        }, senseId), Meta());
        await DataModel.AddChange(ClientId, new SIL.Harmony.Changes.DeleteChange<ExampleSentence>(exampleId), Meta());

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery());

        // The deleted example is gone from the projection; its snippet is recovered from the last snapshot.
        activities.Should().Contain(a => a.ChangeTypes.Contains("delete:ExampleSentence")
            && a.ChangeInfo[0].Subject == "run › to run"
            && a.ChangeInfo[0].Target == "I run daily");
    }

    // --- Comments resolve to whatever their thread is attached to (subject), with the comment text as a
    // truncated target snippet — the same shape as example sentences. ---

    [Fact]
    public async Task ProjectActivity_ChangeInfo_CommentThread_NamesCommentedEntry()
    {
        var entryId = await CreateEntry("run");
        await AddCommentThread(entryId, SubjectType.Entry);

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery());

        // A thread reads at the level of the thing it's on: the commented entry's headword and its owning entry.
        activities.Should().Contain(a => a.ChangeTypes.Contains("CreateCommentThreadChange")
            && a.ChangeInfo[0].Subject == "run"
            && a.ChangeInfo[0].OwningEntryId == entryId);
    }

    [Fact]
    public async Task ProjectActivity_ChangeInfo_UserComment_NamesCommentedSense_AndTextSnippet()
    {
        var entryId = await CreateEntry("run");
        var senseId = await CreateSense(entryId, gloss: "to run", order: 1.0);
        var threadId = await AddCommentThread(senseId, SubjectType.Sense);
        await AddComment(threadId, "looks wrong");

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery());

        // The comment resolves to its sense (subject + owning entry) with the comment text as the target.
        activities.Should().Contain(a => a.ChangeTypes.Contains("CreateUserCommentChange")
            && a.ChangeInfo[0].Subject == "run › to run"
            && a.ChangeInfo[0].OwningEntryId == entryId
            && a.ChangeInfo[0].Target == "looks wrong");
    }

    [Fact]
    public async Task ProjectActivity_ChangeInfo_UserComment_TruncatesLongText()
    {
        var entryId = await CreateEntry("run");
        var threadId = await AddCommentThread(entryId, SubjectType.Entry);
        await AddComment(threadId, "this gloss looks wrong to me");

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery());

        // Long comment text is truncated to the example-snippet budget, backing off to the last whole word.
        activities.Should().Contain(a => a.ChangeTypes.Contains("CreateUserCommentChange")
            && a.ChangeInfo[0].Target == "this gloss looks…");
    }

    [Fact]
    public async Task ProjectActivity_ChangeInfo_DeletedComment_StillHasTextSnippet()
    {
        var entryId = await CreateEntry("run");
        var threadId = await AddCommentThread(entryId, SubjectType.Entry);
        var commentId = await AddComment(threadId, "looks wrong");
        await DataModel.AddChange(ClientId, new SIL.Harmony.Changes.DeleteChange<UserComment>(commentId), Meta());

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery());

        // The deleted comment is gone from the projection; its snippet is recovered from the last snapshot.
        activities.Should().Contain(a => a.ChangeTypes.Contains("delete:UserComment")
            && a.ChangeInfo[0].Subject == "run"
            && a.ChangeInfo[0].Target == "looks wrong");
    }

    // --- Reorder changes name the parent as the subject and (where applicable) the moved child as the target. ---

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
        // Pin to the reorder change: the create-sense commit resolves to the same subject/target.
        activities.Should().Contain(a => a.ChangeTypes.Contains("SetOrderChange:Sense")
            && a.ChangeInfo.Count == 1 && a.ChangeInfo[0].Subject == "run" && a.ChangeInfo[0].Target == "to run");
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

        // Reordering an example names its parent sense as the subject, the example's owning entry (id + headword),
        // and a snippet of the moved example as the target.
        // Pin to the reorder change: the create-example commit resolves to the same subject/owning entry.
        activities.Should().Contain(a => a.ChangeTypes.Contains("SetOrderChange:ExampleSentence")
            && a.ChangeInfo.Count == 1
            && a.ChangeInfo[0].Subject == "run › to run"
            && a.ChangeInfo[0].OwningEntryId == entryId
            && a.ChangeInfo[0].OwningEntryHeadword == "run"
            && a.ChangeInfo[0].Target == "I run");
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
        // Pin to the reorder change: the add-component commit resolves to the same subject/target.
        activities.Should().Contain(a => a.ChangeTypes.Contains("SetOrderChange:ComplexFormComponent")
            && a.ChangeInfo.Count == 1
            && a.ChangeInfo[0].Subject == "blackbird"
            && a.ChangeInfo[0].OwningEntryId == complexFormId
            && a.ChangeInfo[0].Target == "bird");
    }
}
