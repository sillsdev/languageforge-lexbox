using LcmCrdt.Changes;
using SIL.Harmony;
using UUIDNext;

namespace LcmCrdt.Objects;

public static class PreDefinedData
{
    public static readonly Guid CompoundComplexFormTypeId = new("c36f55ed-d1ea-4069-90b3-3f35ff696273");
    public static readonly Guid UnspecifiedComplexFormTypeId = new("eeb78fce-6009-4932-aaa6-85faeb180c69");

    // Part-of-speech GUIDs — canonical, from GOLDEtic.xml in liblcm.
    public static readonly Guid NounPartOfSpeechId = new("a8e41fd3-e343-4c7c-aa05-01ea3dd5cfb5");
    public static readonly Guid VerbPartOfSpeechId = new("86ff66f6-0774-407a-a0dc-3eeaf873daf7");
    public static readonly Guid AdjectivePartOfSpeechId = new("30d07580-5052-4d91-bc24-469b8b2d7df9");
    public static readonly Guid AdverbPartOfSpeechId = new("46e4fe08-ffa0-4c8b-bf98-2c56f38904d9");

    // Seed commit-ids are derived per-project (UUIDv5 namespaced on projectId) so each project
    // owns its own row in LexBox's CrdtCommits table — a shared constant id would collide on the
    // primary key and the seed would get attributed to whichever project pushed first.
    public static Guid ComplexFormTypesSeedCommitId(Guid projectId) =>
        Uuid.NewNameBased(projectId, "complex-form-types-seed");

    public static Guid SemanticDomainsSeedCommitId(Guid projectId) =>
        Uuid.NewNameBased(projectId, "semantic-domains-seed");

    public static Guid PartsOfSpeechSeedCommitId(Guid projectId) =>
        Uuid.NewNameBased(projectId, "parts-of-speech-seed");

    public static Guid CustomViewsSeedCommitId(Guid projectId) =>
        Uuid.NewNameBased(projectId, "custom-views-seed");

    internal static async Task AddPredefinedComplexFormTypes(DataModel dataModel, ProjectData projectData)
    {
        await dataModel.AddChanges(projectData.ClientId,
            [
                new CreateComplexFormType(CompoundComplexFormTypeId, new MultiString() { { "en", "Compound" } } ),
                new CreateComplexFormType(UnspecifiedComplexFormTypeId, new MultiString() { { "en", "Unspecified" } })
            ],
            ComplexFormTypesSeedCommitId(projectData.Id));
    }

    internal static async Task AddPredefinedSemanticDomains(DataModel dataModel, ProjectData projectData)
    {
        //todo load from xml instead of hardcoding and use real IDs
        await dataModel.AddChanges(projectData.ClientId,
            [
                new CreateSemanticDomainChange(new Guid("63403699-07c1-43f3-a47c-069d6e4316e5"), new MultiString() { { "en", "Universe, Creation" } }, "1", true),
                new CreateSemanticDomainChange(new Guid("999581c4-1611-4acb-ae1b-5e6c1dfe6f0c"), new MultiString() { { "en", "Sky" } }, "1.1", true),
                new CreateSemanticDomainChange(new Guid("dc1a2c6f-1b32-4631-8823-36dacc8cb7bb"), new MultiString() { { "en", "World" } }, "1.2", true),
                new CreateSemanticDomainChange(new Guid("1bd42665-0610-4442-8d8d-7c666fee3a6d"), new MultiString() { { "en", "Person" } }, "2", true),
                new CreateSemanticDomainChange(new Guid("46e4fe08-ffa0-4c8b-bf88-2c56f38904d4"), new MultiString() { { "en", "Body" } }, "2.1", false),
                new CreateSemanticDomainChange(new Guid("46e4fe08-ffa0-4c8b-bf88-2c56f38904d5"), new MultiString() { { "en", "Head" } }, "2.1.1", false),
                new CreateSemanticDomainChange(new Guid("46e4fe08-ffa0-4c8b-bf88-2c56f38904d6"), new MultiString() { { "en", "Eye" } }, "2.1.1.1", false),
            ],
            SemanticDomainsSeedCommitId(projectData.Id));
    }

    public static async Task AddPredefinedPartsOfSpeech(DataModel dataModel, ProjectData projectData)
    {
        //todo load from xml instead of hardcoding
        await dataModel.AddChanges(projectData.ClientId,
            [
                new CreatePartOfSpeechChange(NounPartOfSpeechId, new MultiString() { { "en", "Noun" } }, true),
                new CreatePartOfSpeechChange(VerbPartOfSpeechId, new MultiString() { { "en", "Verb" } }, true),
                new CreatePartOfSpeechChange(AdjectivePartOfSpeechId, new MultiString() { { "en", "Adjective" } }, true),
                new CreatePartOfSpeechChange(AdverbPartOfSpeechId, new MultiString() { { "en", "Adverb" } }, true),
            ],
            PartsOfSpeechSeedCommitId(projectData.Id));
    }

    internal static async Task AddPredefinedCustomViews(DataModel dataModel, ProjectData projectData)
    {
        await dataModel.AddChanges(projectData.ClientId,
            [
                new CreateCustomViewChange(
                    new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
                    new CustomView
                    {
                        Id = new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
                        Name = "Minimal",
                        Base = ViewBase.FwLite,
                        EntryFields =
                        [
                            new ViewField { FieldId = "lexemeForm" },
                        ],
                        SenseFields =
                        [
                            new ViewField { FieldId = "gloss" },
                        ],
                        ExampleFields =
                        [
                            new ViewField { FieldId = "sentence" },
                            new ViewField { FieldId = "translations" },
                        ],
                        Vernacular = [new ViewWritingSystem { WsId = "de" }, new ViewWritingSystem { WsId = "de-Zxxx-x-audio" }],
                        Analysis = [new ViewWritingSystem { WsId = "en" }]
                    })
            ],
            CustomViewsSeedCommitId(projectData.Id));
    }

    internal static async Task AddPredefinedMorphTypes(DataModel dataModel, ProjectData projectData)
    {
        await dataModel.AddChanges(projectData.ClientId,
            [.. CanonicalMorphTypes.All.Values.Select(mt => new CreateMorphTypeChange(mt))]);
    }
}
