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
        bool createComponents = true,
        bool createPublications = true)
    {
        var entry = autoFaker.Generate<Entry>();
        if (entryId.HasValue) entry.Id = entryId.Value;
        if (createComponents) await CreateComplexFormComponentEntry(entry, true, entry.Components, api);
        if (createComplexForms) await CreateComplexFormComponentEntry(entry, false, entry.ComplexForms, api);
        if (createComplexFormTypes) await CreateComplexFormTypes(entry.ComplexFormTypes, api);
        if (createPublications) await CreatePublications(entry.PublishIn, api);

        foreach (var sense in entry.Senses)
        {
            sense.EntryId = entry.Id;
            if (sense.PartOfSpeech is not null)
            {
                sense.PartOfSpeechId = sense.PartOfSpeech.Id;
            }
            if (sense.PartOfSpeechId.HasValue)
            {
                var pos = new PartOfSpeech()
                {
                    Id = sense.PartOfSpeechId.Value,
                    Name = { { "en", "generated pos" } }
                };
                await api.CreatePartOfSpeech(pos);
                sense.PartOfSpeech = pos;
            }
            foreach (var senseSemanticDomain in sense.SemanticDomains)
            {
                senseSemanticDomain.Predefined = false;
                await api.CreateSemanticDomain(senseSemanticDomain);
            }
            foreach (var exampleSentence in sense.ExampleSentences)
            {
                exampleSentence.SenseId = sense.Id;
            }
        }
        return entry;
        static async Task CreateComplexFormComponentEntry(Entry entry,
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
                    complexFormComponent.ComplexFormEntryId = entry.Id;
                }
                else
                {
                    complexFormComponent.ComponentEntryId = entry.Id;
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
                    complexFormComponent.ComponentHeadword = createdEntry.Headword();
                    complexFormComponent.ComplexFormHeadword = entry.Headword();
                    complexFormComponent.Order = i++;
                } else
                {
                    complexFormComponent.ComplexFormHeadword = createdEntry.Headword();
                    complexFormComponent.ComponentHeadword = entry.Headword();
                    complexFormComponent.Order = 1;
                }
            }
        }

        static async Task CreateComplexFormTypes(IList<ComplexFormType> complexFormTypes, IMiniLcmApi api)
        {
            foreach (var complexFormType in complexFormTypes)
            {
                await api.CreateComplexFormType(complexFormType);
            }
        }

        static async Task CreatePublications(IList<Publication> publications, IMiniLcmApi api)
        {
            foreach (var publication in publications)
            {
                await api.CreatePublication(publication);
            }
        }
    }
}
