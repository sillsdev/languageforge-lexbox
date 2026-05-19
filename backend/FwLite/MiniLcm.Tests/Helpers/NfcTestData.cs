namespace MiniLcm.Tests.Helpers;

public static class NfcTestData
{
    // U+00EF LATIN SMALL LETTER I WITH DIAERESIS (composed)
    public const string Nfc = "naïve";
    // U+0069 LATIN SMALL LETTER I + U+0308 COMBINING DIAERESIS (decomposed)
    public const string Nfd = "naïve";

    // D: ï C: ï

    public static MultiString CreateNfcMultiString()
    {
        return new() { Values = { { "en", Nfc }, { "fr", Nfc } } };
    }

    public static RichString CreateNfcRichString()
    {
        return new([
            new RichSpan { Text = Nfc, Ws = "en" },
            new RichSpan { Text = Nfc, Ws = "en", Bold = RichTextToggle.On }
        ]);
    }

    public static RichMultiString CreateNfcRichMultiString()
    {
        return new()
        {
            { "en", CreateNfcRichString() },
            { "fr", CreateNfcRichString() }
        };
    }

    public static WritingSystem CreateNfcWritingSystem()
    {
        return new()
        {
            Id = Guid.NewGuid(),
            WsId = "en",
            Type = WritingSystemType.Analysis,
            Name = Nfc,
            Abbreviation = Nfc,
            Font = Nfc,
            Exemplars = [Nfc, Nfc]
        };
    }

    public static PartOfSpeech CreateNfcPartOfSpeech()
    {
        return new()
        {
            Id = Guid.NewGuid(),
            Name = CreateNfcMultiString()
        };
    }

    public static Publication CreateNfcPublication()
    {
        return new()
        {
            Id = Guid.NewGuid(),
            Name = CreateNfcMultiString()
        };
    }

    public static SemanticDomain CreateNfcSemanticDomain()
    {
        return new()
        {
            Id = Guid.NewGuid(),
            Code = Nfc,
            Name = CreateNfcMultiString()
        };
    }

    public static ComplexFormType CreateNfcComplexFormType()
    {
        return new()
        {
            Id = Guid.NewGuid(),
            Name = CreateNfcMultiString()
        };
    }

    public static MorphType CreateNfcMorphType()
    {
        return new()
        {
            Id = Guid.NewGuid(),
            Kind = MorphTypeKind.Stem,
            Name = CreateNfcMultiString(),
            Abbreviation = CreateNfcMultiString(),
            Description = CreateNfcRichMultiString(),
            Prefix = Nfc,
            Postfix = Nfc
        };
    }

    public static Translation CreateNfcTranslation()
    {
        return new()
        {
            Id = Guid.NewGuid(),
            Text = CreateNfcRichMultiString()
        };
    }

    public static ExampleSentence CreateNfcExampleSentence()
    {
        return new()
        {
            Id = Guid.NewGuid(),
            Sentence = CreateNfcRichMultiString(),
            Reference = CreateNfcRichString()
        };
    }

    public static ExampleSentence CreateNfcExampleSentenceWithTranslations()
    {
        return new()
        {
            Id = Guid.NewGuid(),
            Sentence = CreateNfcRichMultiString(),
            Reference = CreateNfcRichString(),
            Translations = [CreateNfcTranslation(), CreateNfcTranslation()]
        };
    }

    public static Sense CreateNfcSense()
    {
        return new()
        {
            Id = Guid.NewGuid(),
            Gloss = CreateNfcMultiString(),
            Definition = CreateNfcRichMultiString()
        };
    }

    public static Sense CreateNfcSenseWithExamples()
    {
        return new()
        {
            Id = Guid.NewGuid(),
            Gloss = CreateNfcMultiString(),
            Definition = CreateNfcRichMultiString(),
            SemanticDomains = [CreateNfcSemanticDomain()],
            PartOfSpeech = CreateNfcPartOfSpeech(),
            ExampleSentences = [CreateNfcExampleSentenceWithTranslations()]
        };
    }

    public static ComplexFormComponent CreateNfcComplexFormComponent()
    {
        return new()
        {
            Id = Guid.NewGuid(),
            ComplexFormEntryId = Guid.NewGuid(),
            ComponentEntryId = Guid.NewGuid(),
            ComplexFormHeadword = Nfc,
            ComponentHeadword = Nfc
        };
    }

    public static Entry CreateNfcEntry()
    {
        return new()
        {
            Id = Guid.NewGuid(),
            LexemeForm = CreateNfcMultiString(),
            CitationForm = CreateNfcMultiString(),
            LiteralMeaning = CreateNfcRichMultiString(),
            Note = CreateNfcRichMultiString()
        };
    }

    public static Entry CreateNfcEntryWithSenses()
    {
        return new()
        {
            Id = Guid.NewGuid(),
            LexemeForm = CreateNfcMultiString(),
            CitationForm = CreateNfcMultiString(),
            LiteralMeaning = CreateNfcRichMultiString(),
            Note = CreateNfcRichMultiString(),
            Senses = [CreateNfcSenseWithExamples()]
        };
    }

    public static Entry CreateNfcEntryWithComponents()
    {
        return new()
        {
            Id = Guid.NewGuid(),
            LexemeForm = CreateNfcMultiString(),
            CitationForm = CreateNfcMultiString(),
            LiteralMeaning = CreateNfcRichMultiString(),
            Note = CreateNfcRichMultiString(),
            Components = [CreateNfcComplexFormComponent()],
            ComplexForms = [CreateNfcComplexFormComponent()]
        };
    }
}
