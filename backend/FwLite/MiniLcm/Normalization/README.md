# String Normalization in MiniLCM

This document explains how we ensure all user-entered text is normalized to NFD (Unicode Normalization Form D - Canonical Decomposition) throughout the system.

## Why NFD Normalization?

Unicode allows the same visual character to be represented in different ways. For example, "naïve" can be:
- **NFC** (Composed): `na\u00EFve` - using single character U+00EF (ï)
- **NFD** (Decomposed): `na\u0069\u0308ve` - using U+0069 (i) + U+0308 (combining diaeresis)

Without normalization, these identical strings won't match in searches or comparisons. We chose NFD because:
1. It's canonical (one representation per character)
2. It works better with combining character input methods
3. It's consistent with FieldWorks (LCModel) behavior

## Architecture

### Write Normalization Wrapper

**Location**: `MiniLcm/Normalization/MiniLcmWriteApiNormalizationWrapper.cs`

This wrapper sits in the API chain and normalizes all user text **before** it reaches the persistence layer:

```
Client → Write Normalization → Read Normalization → Validation → Core API → Database
```

**Key Features:**
- Manually implements ALL write methods (no auto-generation via BeaKona)
- Adding a new write method causes **compile failure** until implemented
- Deep normalization: recursively walks nested objects (Entry → Sense → Example → Translation)
- Separate from read normalization wrapper (can be used independently)

### Read Normalization Wrapper

**Location**: `MiniLcm/Normalization/MiniLcmApiStringNormalizationWrapper.cs`

Normalizes query strings to ensure searches work with both NFC and NFD input.

## What Gets Normalized?

### User-Entered Text (Normalized)
- **MultiString**: Entry.LexemeForm, Sense.Gloss, PartOfSpeech.Name, etc.
- **RichMultiString**: Entry.Note, Sense.Definition, Translation.Text, etc.
- **RichString**: ExampleSentence.Reference
- **Plain strings**: WritingSystem.Name, Abbreviation, Font
- **String arrays**: WritingSystem.Exemplars

### Metadata (NOT Normalized)
- SemanticDomain.Code (codes are identifiers, not user text)
- WritingSystemId (technical identifier)
- File paths, URIs, GUIDs

## FwData Exception

**Important**: FwData APIs are **excluded** from write normalization because LCModel (FieldWorks library) already handles normalization internally. Applying our wrapper would be redundant.

See:
- `FwDataMiniLcmHub.cs` - passes `skipWriteNormalization: true`
- `CrdtFwdataProjectSyncService.cs` - only normalizes CRDT API, not FwData

## Coverage Guarantees

We use multiple strategies to ensure complete coverage:

### 1. Compile-Time Safety ✅
**How**: Manual implementation of all write methods (no BeaKona auto-generation)
**Catches**: New write methods added to interface
**When**: At compile time - won't build until method is implemented

### 2. Reflection-Based Test ✅
**Test**: `WriteNormalizationCoverageTests.AllTextHandlingWriteApiMethods_HaveCorrespondingTests()`
**How**: Uses reflection to verify all write methods have tests
**Catches**: Missing test methods
**When**: At test time

### 3. Object Graph Walker Test ✅
**Test**: `WriteNormalizationCoverageTests.ObjectGraphWalker_VerifiesAllStringsAreNFD()`
**How**: Recursively inspects all object properties for NFC strings after normalization
**Catches**: Missing string fields in models
**When**: At test time

### 4. Wrapper Implementation Verification ✅
**Test**: Part of reflection test - verifies explicit implementations
**How**: Checks wrapper declares all write methods
**Catches**: Methods accidentally forwarded via BeaKona instead of manually implemented
**When**: At test time

## Adding a New Text Field

When you add a new user-entered text field to a model:

1. **It will be caught automatically** by the object graph walker test
2. **Update the normalization wrapper** if it's a new complex type
3. **Run tests** to verify coverage

Example - adding a new field to `Entry`:

```csharp
// In Entry.cs
public virtual MultiString Etymology { get; set; } = new();

// In MiniLcmWriteApiNormalizationWrapper.cs NormalizeEntry()
return new Entry
{
    // ... existing fields ...
    Etymology = StringNormalizer.Normalize(entry.Etymology),  // Add this line
};
```

## Adding a New Write Method

When you add a new method to `IMiniLcmWriteApi`:

1. **Code won't compile** until you implement it in `MiniLcmWriteApiNormalizationWrapper`
2. **Implement the method** and normalize any text parameters
3. **Add a test** in `WriteNormalizationTests.cs`
4. **Run coverage tests** to verify

Example:

```csharp
// In IMiniLcmWriteApi.cs
Task<Etymology> CreateEtymology(Etymology etymology);

// In MiniLcmWriteApiNormalizationWrapper.cs
public async Task<Etymology> CreateEtymology(Etymology etymology)
{
    var normalized = new Etymology
    {
        Id = etymology.Id,
        Source = StringNormalizer.Normalize(etymology.Source),
        // ... normalize all text fields
    };
    return await _api.CreateEtymology(normalized);
}

// In WriteNormalizationTests.cs
[Fact]
public async Task CreateEtymology_NormalizesText()
{
    var etymology = new Etymology
    {
        Id = Guid.NewGuid(),
        Source = new MultiString { Values = { { "en", NFCString } } }
    };

    await NormalizingApi.CreateEtymology(etymology);

    Mock.Get(MockApi).Verify(api => api.CreateEtymology(
        It.Is<Etymology>(e => e.Source.Values["en"] == NFDString)
    ));
}
```

## Testing Strategy

Run normalization tests:
```bash
cd backend/FwLite/MiniLcm.Tests
dotnet test --filter "FullyQualifiedName~WriteNormalization"
```

This runs:
- 21 normalization behavior tests
- 4 coverage verification tests

All tests must pass before merging changes.

## Troubleshooting

### Test Fails: "Found NFC strings after normalization"
- A new string field was added but not normalized
- Check the failure path (e.g., "Entry.Etymology[en]")
- Add normalization in the wrapper's Normalize* method

### Compile Error: "Does not implement interface member"
- A new write method was added to `IMiniLcmWriteApi`
- Implement it in `MiniLcmWriteApiNormalizationWrapper`
- Don't forget to normalize any text parameters

### Test Fails: "Method does not have corresponding normalization test"
- Add a test to `WriteNormalizationTests.cs`
- Or add the method to the skip list if it doesn't handle text (deletes, moves, etc.)

## References

- Unicode Normalization: https://unicode.org/reports/tr15/
- Issue: https://github.com/sillsdev/languageforge-lexbox/issues/[issue-number]
- FwLite Architecture: `backend/FwLite/AGENTS.md`
