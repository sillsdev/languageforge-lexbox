using LcmCrdt.Changes;
using SIL.Harmony;

namespace LcmCrdt.Objects;

public static class PreDefinedData
{
    internal static async Task PredefinedComplexFormTypes(DataModel dataModel, Guid clientId)
    {
        await dataModel.AddChanges(clientId,
            [
                new CreateComplexFormType(new Guid("c36f55ed-d1ea-4069-90b3-3f35ff696273"), new MultiString() { { "en", "Compound" } } ),
                new CreateComplexFormType(new Guid("eeb78fce-6009-4932-aaa6-85faeb180c69"), new MultiString() { { "en", "Unspecified" } })
            ],
            new Guid("dc60d2a9-0cc2-48ed-803c-a238a14b6eae"));
    }

    internal static async Task PredefinedSemanticDomains(DataModel dataModel, Guid clientId)
        {
            //todo load from xml instead of hardcoding and use real IDs
            await dataModel.AddChanges(clientId,
                [
                    new CreateSemanticDomainChange(new Guid("63403699-07c1-43f3-a47c-069d6e4316e5"), new MultiString() { { "en", "Universe, Creation" } }, "1", true),
                    new CreateSemanticDomainChange(new Guid("999581c4-1611-4acb-ae1b-5e6c1dfe6f0c"), new MultiString() { { "en", "Sky" } }, "1.1", true),
                    new CreateSemanticDomainChange(new Guid("dc1a2c6f-1b32-4631-8823-36dacc8cb7bb"), new MultiString() { { "en", "World" } }, "1.2", true),
                    new CreateSemanticDomainChange(new Guid("1bd42665-0610-4442-8d8d-7c666fee3a6d"), new MultiString() { { "en", "Person" } }, "2", true),
                    new CreateSemanticDomainChange(new Guid("46e4fe08-ffa0-4c8b-bf88-2c56f38904d4"), new MultiString() { { "en", "Body" } }, "2.1", false),
                    new CreateSemanticDomainChange(new Guid("46e4fe08-ffa0-4c8b-bf88-2c56f38904d5"), new MultiString() { { "en", "Head" } }, "2.1.1", false),
                    new CreateSemanticDomainChange(new Guid("46e4fe08-ffa0-4c8b-bf88-2c56f38904d6"), new MultiString() { { "en", "Eye" } }, "2.1.1.1", false),
                ],
                new Guid("023faebb-711b-4d2f-a14f-a15621fc66bc"));
        }

    public static async Task PredefinedPartsOfSpeech(DataModel dataModel, Guid clientId)
    {
        //todo load from xml instead of hardcoding
        await dataModel.AddChanges(clientId,
            [
                new CreatePartOfSpeechChange(new Guid("46e4fe08-ffa0-4c8b-bf98-2c56f38904d9"),
                    new MultiString() { { "en", "Adverb" } },
                    true)
            ],
            new Guid("023faebb-711b-4d2f-b34f-a15621fc66bb"));
    }

    internal static async Task PredefinedCustomViews(DataModel dataModel, Guid clientId)
    {
        await dataModel.AddChanges(clientId,
            [
                new CreateCustomViewChange(
                    new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
                    new CustomView
                    {
                        Id = new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
                        Name = "Custom View",
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
                        Vernacular = [new WritingSystemId("de")],
                        Analysis = [new WritingSystemId("en"), new WritingSystemId("en-Zxxx-x-audio")]
                    })
            ],
            new Guid("b2c3d4e5-f6a7-8901-bcde-f12345678901"));
    }
}
