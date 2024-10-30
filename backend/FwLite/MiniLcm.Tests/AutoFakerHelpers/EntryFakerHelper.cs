using MiniLcm.Models;
using Soenneker.Utils.AutoBogus;

namespace MiniLcm.Tests.AutoFakerHelpers;

public static class EntryFakerHelper
{
    public static async Task<Entry> EntryReadyForCreation(this AutoFaker autoFaker,
        IMiniLcmApi api,
        Guid? entryId = null,
        bool createComplexForms = true,
        bool createComplexFormTypes = true,
        bool createComponents = true)
    {
        var entry = autoFaker.Generate<Entry>();
        if (entryId.HasValue) entry.Id = entryId.Value;
        if (createComponents) await CreateComplexFormComponentEntry(entry.Id, true, entry.Components, api);
        if (createComplexForms) await CreateComplexFormComponentEntry(entry.Id, false, entry.ComplexForms, api);
        if (createComplexFormTypes) await CreateComplexFormTypes(entry.ComplexFormTypes, api);
        foreach (var sense in entry.Senses)
        {
            sense.EntryId = entry.Id;
            foreach (var exampleSentence in sense.ExampleSentences)
            {
                exampleSentence.SenseId = sense.Id;
            }
        }
        return entry;
        static async Task CreateComplexFormComponentEntry(Guid entryId,
            bool isComponent,
            IList<ComplexFormComponent> complexFormComponents,
            IMiniLcmApi api)
        {
            int i = 1;
            foreach (var complexFormComponent in complexFormComponents)
            {
                //generated entries won't have the expected ids, so fix them up here
                if (isComponent)
                {
                    complexFormComponent.ComplexFormEntryId = entryId;
                }
                else
                {
                    complexFormComponent.ComponentEntryId = entryId;
                    complexFormComponent.ComponentSenseId = null;
                }

                var name = $"test {(isComponent ? "component" : "complex form")} {i}";
                var createdEntry = await api.CreateEntry(new()
                {
                    Id = isComponent
                        ? complexFormComponent.ComponentEntryId
                        : complexFormComponent.ComplexFormEntryId,
                    LexemeForm = { { "en", name } },
                    Senses =
                    [
                        ..complexFormComponent.ComponentSenseId.HasValue &&
                          isComponent
                            ?
                            [
                                new Sense
                                {
                                    Id = complexFormComponent.ComponentSenseId.Value, Gloss = { { "en", name } }
                                }
                            ]
                            : (ReadOnlySpan<Sense>) []
                    ]
                });
                if (isComponent)
                {
                    complexFormComponent.ComplexFormHeadword = createdEntry.Headword();
                } else
                {
                    complexFormComponent.ComponentHeadword = createdEntry.Headword();
                }
                i++;
            }
        }

        static async Task CreateComplexFormTypes(IList<ComplexFormType> complexFormTypes, IMiniLcmApi api)
        {
            foreach (var complexFormType in complexFormTypes)
            {
                await api.CreateComplexFormType(complexFormType);
            }
        }
    }
}
