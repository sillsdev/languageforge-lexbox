using LcmCrdt.Changes;
using SIL.Harmony;

namespace LcmCrdt.Objects;

public static class PreDefinedData
{
    internal static async Task PredefinedSemanticDomains(DataModel dataModel, Guid clientId)
        {
            //todo load from xml instead of hardcoding and use real IDs
            await dataModel.AddChanges(clientId,
                [
                    new CreateSemanticDomainChange(new Guid("46e4fe08-ffa0-4c8b-bf88-2c56f38904d0"), new MultiString() { { "en", "Universe, Creation" } }, "1", true),
                    new CreateSemanticDomainChange(new Guid("46e4fe08-ffa0-4c8b-bf88-2c56f38904d1"), new MultiString() { { "en", "Sky" } }, "1.1", true),
                    new CreateSemanticDomainChange(new Guid("46e4fe08-ffa0-4c8b-bf88-2c56f38904d2"), new MultiString() { { "en", "World" } }, "1.2", true),
                    new CreateSemanticDomainChange(new Guid("46e4fe08-ffa0-4c8b-bf88-2c56f38904d3"), new MultiString() { { "en", "Person" } }, "2", true),
                    new CreateSemanticDomainChange(new Guid("46e4fe08-ffa0-4c8b-bf88-2c56f38904d4"), new MultiString() { { "en", "Body" } }, "2.1", true),
                    new CreateSemanticDomainChange(new Guid("46e4fe08-ffa0-4c8b-bf88-2c56f38904d5"), new MultiString() { { "en", "Head" } }, "2.1.1", true),
                    new CreateSemanticDomainChange(new Guid("46e4fe08-ffa0-4c8b-bf88-2c56f38904d6"), new MultiString() { { "en", "Eye" } }, "2.1.1.1", true),
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
}
