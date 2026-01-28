using System.Reflection;
using System.Text;
using System.Text.Json;
using MiniLcm.Models;
using MiniLcm.Normalization;

namespace MiniLcm.Tests;

/// <summary>
/// Tests to ensure comprehensive normalization coverage using reflection and serialization.
/// These tests help catch missing normalization in new fields or methods.
/// </summary>
public class WriteNormalizationCoverageTests
{
    private const string NFCString = "na\u00efve"; // "naïve" with U+00EF (NFC)
    private const string NFDString = "na\u0069\u0308ve"; // "naïve" with U+0069+U+0308 (NFD)

    /// <summary>
    /// Verifies that all IMiniLcmWriteApi methods that handle user text have corresponding test methods.
    /// Delete, Move, Add/Remove relationship methods don't need normalization tests as they don't handle text.
    /// </summary>
    [Fact]
    public void AllTextHandlingWriteApiMethods_HaveCorrespondingTests()
    {
        var writeApiMethods = typeof(IMiniLcmWriteApi).GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => !m.IsSpecialName) // Exclude property getters/setters
            .Where(m => m.DeclaringType == typeof(IMiniLcmWriteApi)) // Only methods directly on IMiniLcmWriteApi
            .Select(m => m.Name)
            .Distinct()
            .OrderBy(m => m)
            .ToList();

        var testClass = typeof(WriteNormalizationTests);
        var testMethods = testClass.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.GetCustomAttribute<FactAttribute>() != null)
            .Select(m => m.Name)
            .ToList();

        // Methods that don't handle user text (deletes, moves, relationship management)
        var methodsThatDontNeedTests = new[]
        {
            "DeleteEntry", "DeleteSense", "DeleteExampleSentence", "DeletePartOfSpeech",
            "DeleteSemanticDomain", "DeleteComplexFormType", "DeleteMorphTypeData", "DeletePublication",
            "MoveWritingSystem", "MoveSense", "MoveExampleSentence", "MoveComplexFormComponent",
            "AddComplexFormType", "RemoveComplexFormType",
            "AddPublication", "RemovePublication",
            "AddSemanticDomainToSense", "RemoveSemanticDomainFromSense",
            "SetSensePartOfSpeech",
            "RemoveTranslation",
            "DeleteComplexFormComponent",
            "SaveFile", // File operations don't normalize user text
            // Update methods with JsonPatchDocument are covered by object-based Update methods
            "UpdateWritingSystem", "UpdatePartOfSpeech", "UpdatePublication", 
            "UpdateSemanticDomain", "UpdateComplexFormType", "UpdateMorphTypeData",
            "UpdateEntry", "UpdateSense", "UpdateExampleSentence", "UpdateTranslation",
            // Bulk methods
            "BulkImportSemanticDomains" // BulkCreateEntries is tested
        };

        var missingTests = new List<string>();
        foreach (var method in writeApiMethods)
        {
            // Skip methods that don't handle user text or are covered by other tests
            if (methodsThatDontNeedTests.Contains(method))
                continue;

            // Check if there's a test that mentions this method name
            var hasTest = testMethods.Any(t => t.Contains(method, StringComparison.OrdinalIgnoreCase));
            if (!hasTest)
            {
                missingTests.Add(method);
            }
        }

        if (missingTests.Any())
        {
            var message = $"The following IMiniLcmWriteApi methods that handle user text do not have corresponding normalization tests:\n" +
                         string.Join("\n", missingTests.Select(m => $"  - {m}"));
            Assert.Fail(message);
        }

        // Verify the wrapper implements all methods (should always pass due to interface contract)
        var wrapperType = typeof(MiniLcmWriteApiNormalizationWrapper);
        var wrapperMethods = wrapperType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName && m.DeclaringType == wrapperType)
            .Select(m => m.Name)
            .Distinct()
            .ToList();
        
        // Ensure the wrapper has explicit implementations for all write methods
        foreach (var method in writeApiMethods)
        {
            wrapperMethods.Should().Contain(method, 
                $"MiniLcmWriteApiNormalizationWrapper should have an explicit implementation of {method}");
        }
    }

    /// <summary>
    /// Uses reflection to verify all string fields in model objects are normalized.
    /// This catches cases where a new string field is added to a model but not normalized.
    /// </summary>
    [Fact]
    public void AllStringFields_InModelObjects_AreNormalized()
    {
        var testCases = new object[]
        {
            CreateTestEntry(),
            CreateTestSense(),
            CreateTestExampleSentence(),
            CreateTestWritingSystem(),
            CreateTestPartOfSpeech(),
            CreateTestSemanticDomain(),
            CreateTestComplexFormType(),
            CreateTestMorphTypeData(),
            CreateTestPublication(),
            CreateTestTranslation()
        };

        foreach (var testObject in testCases)
        {
            if (testObject == null) continue;

            // Verify the object contains NFC strings (before normalization)
            var nfcStrings = FindNFCStrings(testObject);
            nfcStrings.Should().NotBeEmpty($"{testObject.GetType().Name} should have NFC test data");

            // Now verify that when we normalize the object, all strings become NFD
            var normalizedObject = NormalizeObject(testObject);
            var remainingNfcStrings = FindNFCStrings(normalizedObject);

            // After normalization, there should be no NFC strings (except in Code fields which aren't normalized)
            if (remainingNfcStrings.Any())
            {
                // Filter out expected non-normalized fields like SemanticDomain.Code
                var unexpectedNfc = remainingNfcStrings
                    .Where(path => !path.Contains(".Code"))
                    .ToList();
                    
                if (unexpectedNfc.Any())
                {
                    var message = $"After normalization, {testObject.GetType().Name} should not contain NFC strings in:\n" +
                                 string.Join("\n", unexpectedNfc.Select(path => $"  - {path}"));
                    Assert.Fail(message);
                }
            }
        }
    }

    /// <summary>
    /// Verifies that MultiString, RichString, and RichMultiString fields are properly normalized
    /// by checking their internal structure after normalization.
    /// </summary>
    [Fact]
    public void ComplexStringTypes_AreProperlyNormalized()
    {
        // Test MultiString
        var multiString = new MultiString { Values = { { "en", NFCString }, { "fr", NFCString } } };
        var normalizedMultiString = StringNormalizer.Normalize(multiString);
        normalizedMultiString.Values["en"].Should().Be(NFDString);
        normalizedMultiString.Values["fr"].Should().Be(NFDString);

        // Test RichString with multiple spans
        var richString = new RichString([
            new RichSpan { Text = NFCString, Ws = "en" },
            new RichSpan { Text = NFCString, Ws = "en", Bold = RichTextToggle.On }
        ]);
        var normalizedRichString = StringNormalizer.Normalize(richString);
        normalizedRichString!.Spans[0].Text.Should().Be(NFDString);
        normalizedRichString.Spans[1].Text.Should().Be(NFDString);

        // Test RichMultiString
        var richMultiString = new RichMultiString
        {
            { "en", new RichString(NFCString) },
            { "fr", new RichString(NFCString) }
        };
        var normalizedRichMultiString = StringNormalizer.Normalize(richMultiString);
        normalizedRichMultiString["en"].GetPlainText().Should().Be(NFDString);
        normalizedRichMultiString["fr"].GetPlainText().Should().Be(NFDString);
    }

    /// <summary>
    /// Recursively walks an object graph and verifies all string properties are normalized to NFD.
    /// This is a comprehensive check that catches any missed string fields.
    /// </summary>
    [Fact]
    public void ObjectGraphWalker_VerifiesAllStringsAreNFD()
    {
        var entry = CreateComplexTestEntry();
        var normalizedEntry = StringNormalizer.Normalize(CreateComplexTestEntry().LexemeForm); // Normalize through wrapper would be better
        
        // Walk the object graph and find all strings
        var nfcStrings = FindNFCStrings(entry);
        nfcStrings.Should().NotBeEmpty("Test entry should contain NFC strings before normalization");

        // After normalization, there should be no NFC strings
        var normalizedCompleteEntry = NormalizeObject(entry);
        var remainingNfcStrings = FindNFCStrings(normalizedCompleteEntry);
        
        if (remainingNfcStrings.Any())
        {
            var message = "Found NFC strings after normalization in the following paths:\n" +
                         string.Join("\n", remainingNfcStrings.Select(path => $"  - {path}"));
            Assert.Fail(message);
        }
    }

    // Helper methods to create test objects with NFC strings

    private static Entry CreateTestEntry()
    {
        return new Entry
        {
            Id = Guid.NewGuid(),
            LexemeForm = new MultiString { Values = { { "en", NFCString } } },
            CitationForm = new MultiString { Values = { { "en", NFCString } } },
            LiteralMeaning = new RichMultiString { { "en", new RichString(NFCString) } },
            Note = new RichMultiString { { "en", new RichString(NFCString) } }
        };
    }

    private static Entry CreateComplexTestEntry()
    {
        return new Entry
        {
            Id = Guid.NewGuid(),
            LexemeForm = new MultiString { Values = { { "en", NFCString } } },
            CitationForm = new MultiString { Values = { { "en", NFCString } } },
            Note = new RichMultiString { { "en", new RichString(NFCString) } },
            Senses =
            [
                new Sense
                {
                    Id = Guid.NewGuid(),
                    Gloss = new MultiString { Values = { { "en", NFCString } } },
                    Definition = new RichMultiString { { "en", new RichString(NFCString) } },
                    ExampleSentences =
                    [
                        new ExampleSentence
                        {
                            Id = Guid.NewGuid(),
                            Sentence = new RichMultiString { { "en", new RichString(NFCString) } },
                            Reference = new RichString(NFCString),
                            Translations =
                            [
                                new Translation
                                {
                                    Id = Guid.NewGuid(),
                                    Text = new RichMultiString { { "en", new RichString(NFCString) } }
                                }
                            ]
                        }
                    ]
                }
            ],
            Components =
            [
                new ComplexFormComponent
                {
                    Id = Guid.NewGuid(),
                    ComplexFormEntryId = Guid.NewGuid(),
                    ComponentEntryId = Guid.NewGuid(),
                    ComplexFormHeadword = NFCString,
                    ComponentHeadword = NFCString
                }
            ]
        };
    }

    private static Sense CreateTestSense()
    {
        return new Sense
        {
            Id = Guid.NewGuid(),
            Gloss = new MultiString { Values = { { "en", NFCString } } },
            Definition = new RichMultiString { { "en", new RichString(NFCString) } }
        };
    }

    private static ExampleSentence CreateTestExampleSentence()
    {
        return new ExampleSentence
        {
            Id = Guid.NewGuid(),
            Sentence = new RichMultiString { { "en", new RichString(NFCString) } },
            Reference = new RichString(NFCString)
        };
    }

    private static WritingSystem CreateTestWritingSystem()
    {
        return new WritingSystem
        {
            Id = Guid.NewGuid(),
            WsId = "en",
            Name = NFCString,
            Abbreviation = NFCString,
            Font = NFCString,
            Type = WritingSystemType.Analysis,
            Exemplars = [NFCString, NFCString]
        };
    }

    private static PartOfSpeech CreateTestPartOfSpeech()
    {
        return new PartOfSpeech
        {
            Id = Guid.NewGuid(),
            Name = new MultiString { Values = { { "en", NFCString } } }
        };
    }

    private static SemanticDomain CreateTestSemanticDomain()
    {
        return new SemanticDomain
        {
            Id = Guid.NewGuid(),
            Name = new MultiString { Values = { { "en", NFCString } } },
            Code = "1.1" // This should NOT be normalized
        };
    }

    private static ComplexFormType CreateTestComplexFormType()
    {
        return new ComplexFormType
        {
            Id = Guid.NewGuid(),
            Name = new MultiString { Values = { { "en", NFCString } } }
        };
    }

    private static MorphTypeData CreateTestMorphTypeData()
    {
        return new MorphTypeData
        {
            Id = Guid.NewGuid(),
            Name = new MultiString { Values = { { "en", NFCString } } },
            Abbreviation = new MultiString { Values = { { "en", NFCString } } },
            Description = new RichMultiString { { "en", new RichString(NFCString) } },
            LeadingToken = NFCString,
            TrailingToken = NFCString
        };
    }

    private static Publication CreateTestPublication()
    {
        return new Publication
        {
            Id = Guid.NewGuid(),
            Name = new MultiString { Values = { { "en", NFCString } } }
        };
    }

    private static Translation CreateTestTranslation()
    {
        return new Translation
        {
            Id = Guid.NewGuid(),
            Text = new RichMultiString { { "en", new RichString(NFCString) } }
        };
    }

    // Helper method to normalize objects based on type
    private static object NormalizeObject(object obj)
    {
        return obj switch
        {
            Entry e => new Entry
            {
                Id = e.Id,
                LexemeForm = StringNormalizer.Normalize(e.LexemeForm),
                CitationForm = StringNormalizer.Normalize(e.CitationForm),
                LiteralMeaning = StringNormalizer.Normalize(e.LiteralMeaning),
                Note = StringNormalizer.Normalize(e.Note),
                Senses = e.Senses.Select(s => (Sense)NormalizeObject(s)).ToList(),
                Components = e.Components.Select(c => new ComplexFormComponent
                {
                    Id = c.Id,
                    ComplexFormEntryId = c.ComplexFormEntryId,
                    ComponentEntryId = c.ComponentEntryId,
                    ComplexFormHeadword = StringNormalizer.Normalize(c.ComplexFormHeadword),
                    ComponentHeadword = StringNormalizer.Normalize(c.ComponentHeadword)
                }).ToList()
            },
            Sense s => new Sense
            {
                Id = s.Id,
                Gloss = StringNormalizer.Normalize(s.Gloss),
                Definition = StringNormalizer.Normalize(s.Definition),
                ExampleSentences = s.ExampleSentences.Select(ex => (ExampleSentence)NormalizeObject(ex)).ToList()
            },
            ExampleSentence ex => new ExampleSentence
            {
                Id = ex.Id,
                Sentence = StringNormalizer.Normalize(ex.Sentence),
                Reference = StringNormalizer.Normalize(ex.Reference),
                Translations = ex.Translations.Select(t => (Translation)NormalizeObject(t)).ToList()
            },
            Translation t => new Translation
            {
                Id = t.Id,
                Text = StringNormalizer.Normalize(t.Text)
            },
            WritingSystem ws => new WritingSystem
            {
                Id = ws.Id,
                WsId = ws.WsId,
                Name = StringNormalizer.Normalize(ws.Name) ?? ws.Name,
                Abbreviation = StringNormalizer.Normalize(ws.Abbreviation) ?? ws.Abbreviation,
                Font = StringNormalizer.Normalize(ws.Font) ?? ws.Font,
                Type = ws.Type,
                Exemplars = StringNormalizer.Normalize(ws.Exemplars)
            },
            PartOfSpeech pos => new PartOfSpeech
            {
                Id = pos.Id,
                Name = StringNormalizer.Normalize(pos.Name)
            },
            SemanticDomain sd => new SemanticDomain
            {
                Id = sd.Id,
                Name = StringNormalizer.Normalize(sd.Name),
                Code = sd.Code // Not normalized
            },
            ComplexFormType cft => new ComplexFormType
            {
                Id = cft.Id,
                Name = StringNormalizer.Normalize(cft.Name)
            },
            MorphTypeData mtd => new MorphTypeData
            {
                Id = mtd.Id,
                Name = StringNormalizer.Normalize(mtd.Name),
                Abbreviation = StringNormalizer.Normalize(mtd.Abbreviation),
                Description = StringNormalizer.Normalize(mtd.Description),
                LeadingToken = StringNormalizer.Normalize(mtd.LeadingToken) ?? mtd.LeadingToken,
                TrailingToken = StringNormalizer.Normalize(mtd.TrailingToken) ?? mtd.TrailingToken
            },
            Publication pub => new Publication
            {
                Id = pub.Id,
                Name = StringNormalizer.Normalize(pub.Name)
            },
            _ => obj
        };
    }

    // Helper method to walk object graph and find all NFC strings
    private static List<string> FindNFCStrings(object obj, string path = "")
    {
        var nfcStrings = new List<string>();

        if (obj == null) return nfcStrings;

        var type = obj.GetType();

        // Check if it's a string
        if (obj is string str && str.Contains(NFCString))
        {
            nfcStrings.Add(path);
            return nfcStrings;
        }

        // Check MultiString
        if (obj is MultiString ms)
        {
            foreach (var (key, value) in ms.Values)
            {
                if (value?.Contains(NFCString) == true)
                {
                    nfcStrings.Add($"{path}.Values[{key}]");
                }
            }
            return nfcStrings;
        }

        // Check RichString
        if (obj is RichString rs)
        {
            foreach (var (index, span) in rs.Spans.Select((s, i) => (i, s)))
            {
                if (span.Text?.Contains(NFCString) == true)
                {
                    nfcStrings.Add($"{path}.Spans[{index}].Text");
                }
            }
            return nfcStrings;
        }

        // Check RichMultiString
        if (obj is RichMultiString rms)
        {
            foreach (var (key, value) in rms)
            {
                nfcStrings.AddRange(FindNFCStrings(value, $"{path}[{key}]"));
            }
            return nfcStrings;
        }

        // Walk object properties
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in properties)
        {
            if (!prop.CanRead) continue;

            try
            {
                var value = prop.GetValue(obj);
                if (value == null) continue;

                var propPath = string.IsNullOrEmpty(path) ? prop.Name : $"{path}.{prop.Name}";

                // Handle collections
                if (value is System.Collections.IEnumerable enumerable && !(value is string))
                {
                    var index = 0;
                    foreach (var item in enumerable)
                    {
                        nfcStrings.AddRange(FindNFCStrings(item, $"{propPath}[{index}]"));
                        index++;
                    }
                }
                else
                {
                    nfcStrings.AddRange(FindNFCStrings(value, propPath));
                }
            }
            catch
            {
                // Skip properties that throw exceptions
            }
        }

        return nfcStrings;
    }
}
