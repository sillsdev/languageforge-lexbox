using LcmCrdt.Objects;
using Microsoft.Extensions.DependencyInjection;

namespace LcmCrdt;

internal static class ExampleProjectData
{
    // Parts of speech and complex form types ship in the template (CreateProjectFromTemplate).
    // The Noun PartOfSpeech keeps liblcm's canonical Id (== PreDefinedData.NounPartOfSpeechId), so
    // we reference that directly; the Compound complex-form-type Id differs from PreDefinedData's, so
    // CreateBerryComplexForms resolves it by name.

    public static async Task Seed(IServiceProvider provider, CrdtProject _)
    {
        var api = provider.GetRequiredService<IMiniLcmApi>();
        await CreateWritingSystems(api);
        var beere = await CreateFruitEntries(api);
        await CreateBerryComplexForms(api, beere);
    }

    private static async Task CreateWritingSystems(IMiniLcmApi api)
    {
        // The template path already provides vernacular "de" and analysis "en"; bring them to the demo's
        // presentation (friendly name, Arial, Latin exemplars) in place rather than re-create them, which
        // CreateWritingSystem rejects as duplicates. The remaining three don't exist yet, so create them —
        // appended after the existing ones, preserving the order users see (de, de-audio, de-ipa / en, fr).
        await api.UpdateWritingSystem("de", WritingSystemType.Vernacular, new UpdateObjectInput<WritingSystem>()
            .Set(ws => ws.Name, "German")
            .Set(ws => ws.Abbreviation, "de")
            .Set(ws => ws.Font, "Arial")
            .Set(ws => ws.Exemplars, WritingSystem.LatinExemplars));
        await api.CreateWritingSystem(new()
        {
            Id = Guid.NewGuid(),
            Type = WritingSystemType.Vernacular,
            WsId = "de-Zxxx-x-audio",
            Name = "German (A)",
            Abbreviation = "Deu 🔊",
            Font = "Arial",
            Exemplars = WritingSystem.LatinExemplars
        });
        await api.CreateWritingSystem(new()
        {
            Id = Guid.NewGuid(),
            Type = WritingSystemType.Vernacular,
            WsId = "de-fonipa",
            Name = "German (IPA)",
            Abbreviation = "de IPA",
            Font = "Arial",
            Exemplars = WritingSystem.LatinExemplars
        });
        await api.UpdateWritingSystem("en", WritingSystemType.Analysis, new UpdateObjectInput<WritingSystem>()
            .Set(ws => ws.Name, "English")
            .Set(ws => ws.Abbreviation, "en")
            .Set(ws => ws.Font, "Arial")
            .Set(ws => ws.Exemplars, WritingSystem.LatinExemplars));
        await api.CreateWritingSystem(new()
        {
            Id = Guid.NewGuid(),
            Type = WritingSystemType.Analysis,
            WsId = "fr",
            Name = "French",
            Abbreviation = "fr",
            Font = "Arial",
            Exemplars = WritingSystem.LatinExemplars
        });
    }

    private static async Task<Entry> CreateFruitEntries(IMiniLcmApi api)
    {
        await api.CreateEntry(new()
        {
            Id = Guid.NewGuid(),
            LexemeForm = { ["de"] = "Apfel", ["de-fonipa"] = "ˈapfəl" },
            CitationForm = { ["de"] = "Apfel", ["de-fonipa"] = "ˈapfəl" },
            LiteralMeaning = { ["en"] = new RichString("Fruit") },
            Senses =
            [
                new()
                {
                    PartOfSpeechId = PreDefinedData.NounPartOfSpeechId,
                    Gloss = { ["en"] = "Apple", ["fr"] = "Pomme" },
                    Definition =
                    {
                        ["en"] = new RichString(
                            "fruit with red, yellow, or green skin with a sweet or tart crispy white flesh")
                    },
                    ExampleSentences =
                    [
                        new()
                        {
                            Sentence = { ["de"] = new RichString("Wir haben einen Apfel gegessen") },
                            Translations =
                            [
                                new() { Text = { ["en"] = new RichString("We ate an apple") } }
                            ]
                        }
                    ]
                }
            ]
        });

        await api.CreateEntry(new()
        {
            Id = Guid.NewGuid(),
            LexemeForm = { ["de"] = "Banane", ["de-fonipa"] = "baˈnaːnə" },
            CitationForm = { ["de"] = "Banane", ["de-fonipa"] = "baˈnaːnə" },
            LiteralMeaning = { ["en"] = new RichString("Fruit") },
            Senses =
            [
                new()
                {
                    PartOfSpeechId = PreDefinedData.NounPartOfSpeechId,
                    Gloss = { ["en"] = "Banana", ["fr"] = "Banane" },
                    Definition = { ["en"] = new RichString("long curved fruit with yellow skin and soft sweet flesh") },
                    ExampleSentences =
                    [
                        new()
                        {
                            Sentence = { ["de"] = new RichString("Der Affe hat eine Banane geschält") },
                            Translations =
                            [
                                new() { Text = { ["en"] = new RichString("The monkey peeled a banana") } }
                            ]
                        }
                    ]
                }
            ]
        });

        await api.CreateEntry(new()
        {
            Id = Guid.NewGuid(),
            LexemeForm = { ["de"] = "Orange", ["de-fonipa"] = "oˈʁãːʒə" },
            CitationForm = { ["de"] = "Orange", ["de-fonipa"] = "oˈʁãːʒə" },
            LiteralMeaning = { ["en"] = new RichString("Fruit") },
            Senses =
            [
                new()
                {
                    PartOfSpeechId = PreDefinedData.NounPartOfSpeechId,
                    Gloss = { ["en"] = "Orange", ["fr"] = "Orange" },
                    Definition =
                    {
                        ["en"] = new RichString("round citrus fruit with orange skin and juicy segments inside")
                    },
                    ExampleSentences =
                    [
                        new()
                        {
                            Sentence = { ["de"] = new RichString("Ich habe die Orange für Saft ausgepresst") },
                            Translations =
                            [
                                new() { Text = { ["en"] = new RichString("I squeezed the orange for juice") } }
                            ]
                        }
                    ]
                }
            ]
        });

        await api.CreateEntry(new()
        {
            Id = Guid.NewGuid(),
            LexemeForm = { ["de"] = "Traube", ["de-fonipa"] = "ˈtʁaʊ̯bə" },
            CitationForm = { ["de"] = "Traube", ["de-fonipa"] = "ˈtʁaʊ̯bə" },
            LiteralMeaning = { ["en"] = new RichString("Fruit") },
            Senses =
            [
                new()
                {
                    PartOfSpeechId = PreDefinedData.NounPartOfSpeechId,
                    Gloss = { ["en"] = "Grape", ["fr"] = "Raisin" },
                    Definition =
                    {
                        ["en"] = new RichString(
                            "small round or oval fruit growing in clusters, used for wine or eating")
                    },
                    ExampleSentences =
                    [
                        new()
                        {
                            Sentence = { ["de"] = new RichString("Der Weinberg war voller reifer Trauben") },
                            Translations =
                            [
                                new() { Text = { ["en"] = new RichString("The vineyard was full of ripe grapes") } }
                            ]
                        }
                    ]
                }
            ]
        });

        return await api.CreateEntry(new()
        {
            Id = Guid.NewGuid(),
            LexemeForm = { ["de"] = "Beere", ["de-fonipa"] = "ˈbeːʁə" },
            CitationForm = { ["de"] = "Beere", ["de-fonipa"] = "ˈbeːʁə" },
            LiteralMeaning = { ["en"] = new RichString("Fruit") },
            Senses =
            [
                new()
                {
                    PartOfSpeechId = PreDefinedData.NounPartOfSpeechId,
                    Gloss = { ["en"] = "Berry", ["fr"] = "Baie" },
                    Definition =
                    {
                        ["en"] = new RichString("small juicy fruit, typically round and brightly coloured")
                    },
                    ExampleSentences =
                    [
                        new()
                        {
                            Sentence = { ["de"] = new RichString("Im Wald wachsen viele Beeren") },
                            Translations =
                            [
                                new() { Text = { ["en"] = new RichString("Many berries grow in the forest") } }
                            ]
                        }
                    ]
                }
            ]
        });
    }

    private static async Task CreateBerryComplexForms(IMiniLcmApi api, Entry beere)
    {
        var compoundType = await api.GetComplexFormTypes().FirstAsync(ct => ct.Name["en"] == "Compound");

        var erdbeere = new Entry
        {
            Id = Guid.NewGuid(),
            LexemeForm = { ["de"] = "Erdbeere", ["de-fonipa"] = "ˈeːɐ̯tˌbeːʁə" },
            CitationForm = { ["de"] = "Erdbeere", ["de-fonipa"] = "ˈeːɐ̯tˌbeːʁə" },
            LiteralMeaning = { ["en"] = new RichString("Fruit") },
            Senses =
            [
                new()
                {
                    PartOfSpeechId = PreDefinedData.NounPartOfSpeechId,
                    Gloss = { ["en"] = "Strawberry", ["fr"] = "Fraise" },
                    Definition =
                    {
                        ["en"] = new RichString(
                            "sweet red berry with seeds on its surface, grown on a low plant")
                    }
                }
            ]
        };
        erdbeere.Components = [ComplexFormComponent.FromEntries(erdbeere, beere)];
        await api.CreateEntry(erdbeere);
        await api.AddComplexFormType(erdbeere.Id, compoundType.Id);

        var heidelbeere = new Entry
        {
            Id = Guid.NewGuid(),
            LexemeForm = { ["de"] = "Heidelbeere", ["de-fonipa"] = "ˈhaɪ̯dl̩ˌbeːʁə" },
            CitationForm = { ["de"] = "Heidelbeere", ["de-fonipa"] = "ˈhaɪ̯dl̩ˌbeːʁə" },
            LiteralMeaning = { ["en"] = new RichString("Fruit") },
            Senses =
            [
                new()
                {
                    PartOfSpeechId = PreDefinedData.NounPartOfSpeechId,
                    Gloss = { ["en"] = "Blueberry", ["fr"] = "Myrtille" },
                    Definition = { ["en"] = new RichString("small blue-purple berry that grows on a shrub") }
                }
            ]
        };
        heidelbeere.Components = [ComplexFormComponent.FromEntries(heidelbeere, beere)];
        await api.CreateEntry(heidelbeere);
        await api.AddComplexFormType(heidelbeere.Id, compoundType.Id);
    }
}
