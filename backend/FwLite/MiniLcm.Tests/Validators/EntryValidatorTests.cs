using FluentValidation.TestHelper;
using MiniLcm.Validators;

namespace MiniLcm.Tests.Validators;

public class EntryValidatorTests
{
    private readonly EntryValidator _validator = new();

    [Fact]
    public void Succeeds_WhenSenseEntryIdIsGuidEmpty()
    {
        var entryId = Guid.NewGuid();
        var entry = new Entry() { Id = entryId, LexemeForm = new MultiString(){{"en", "lexeme"}}, Senses = [new Sense() { EntryId = Guid.Empty, }] };
        _validator.TestValidate(entry).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Succeeds_WhenSenseEntryIdMatchesEntry()
    {
        var entryId = Guid.NewGuid();
        var entry = new Entry() { Id = entryId, LexemeForm = new MultiString(){{"en", "lexeme"}}, Senses = [new Sense() { EntryId = entryId, }] };
        _validator.TestValidate(entry).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Fails_WhenSenseEntryIdDoesNotMatchEntry()
    {
        var entryId = Guid.NewGuid();
        var entry = new Entry() { Id = entryId, LexemeForm = new MultiString(){{"en", "lexeme"}}, Senses = [new Sense() { EntryId = Guid.NewGuid(), }] };
        _validator.TestValidate(entry).ShouldHaveValidationErrorFor("Senses[0].EntryId");
    }

    [Fact]
    public void Succeeds_WhenDeletedAtIsNull()
    {
        var entryId = Guid.NewGuid();
        var entry = new Entry() { Id = entryId, DeletedAt = null, LexemeForm = new MultiString(){{"en", "lexeme"}}, Senses = [new Sense() { EntryId = entryId, }] };
        _validator.TestValidate(entry).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Fails_WhenDeletedAtIsNotNull()
    {
        var entryId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var entry = new Entry() { Id = entryId, DeletedAt = now, LexemeForm = new MultiString(){{"en", "lexeme"}}, Senses = [new Sense() { EntryId = Guid.Empty, }] };
        _validator.TestValidate(entry).ShouldHaveValidationErrorFor("DeletedAt");
    }

    [Fact]
    public void Succeeds_WhenLexemeFormIsPresent()
    {
        var entry = new Entry() { Id = Guid.NewGuid(), LexemeForm = new MultiString(){{"en", "lexeme"}} };
        _validator.TestValidate(entry).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Fails_WhenLexemeFormIsMissing()
    {
        var entry = new Entry() { Id = Guid.NewGuid() };
        _validator.TestValidate(entry).ShouldHaveValidationErrorFor("LexemeForm");
    }

    [Fact]
    public void Fails_WhenLexemeFormHasNoContent()
    {
        // Technically the same as Fails_WhenLexemeFormIsMissing -- should we combine them?
        var entry = new Entry() { Id = Guid.NewGuid(), LexemeForm = new MultiString() };
        _validator.TestValidate(entry).ShouldHaveValidationErrorFor("LexemeForm");
    }

    [Fact]
    public void Fails_WhenLexemeFormHasWsWithEmptyContent()
    {
        var entry = new Entry() { Id = Guid.NewGuid(), LexemeForm = new MultiString(){{"en", ""}} };
        _validator.TestValidate(entry).ShouldHaveValidationErrorFor("LexemeForm");
    }
//

    [Fact]
    public void Succeeds_WhenCitationFormIsPresent()
    {
        var entry = new Entry() { Id = Guid.NewGuid(), LexemeForm = new MultiString(){{"en", "lexeme"}}, CitationForm = new MultiString(){{"en", "citation"}} };
        _validator.TestValidate(entry).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Succeeds_WhenCitationFormIsMissing()
    {
        var entry = new Entry() { Id = Guid.NewGuid(), LexemeForm = new MultiString(){{"en", "lexeme"}} };
        _validator.TestValidate(entry).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Succeeds_WhenCitationFormHasNoContent()
    {
        // Technically the same as Succeeds_WhenCitationFormIsMissing -- should we combine them?
        var entry = new Entry() { Id = Guid.NewGuid(), LexemeForm = new MultiString(){{"en", "lexeme"}}, CitationForm = new MultiString() };
        _validator.TestValidate(entry).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Fails_WhenCitationFormHasWsWithEmptyContent()
    {
        var entry = new Entry() { Id = Guid.NewGuid(), LexemeForm = new MultiString(){{"en", "lexeme"}}, CitationForm = new MultiString(){{"en", ""}} };
        _validator.TestValidate(entry).ShouldHaveValidationErrorFor("CitationForm");
    }

    // TODO: That's extremely similar to the LexemeForm tests except for the missing/no content tests.
    // And we'll need to write similar tests for LiteralMeaning and Note. A test helper method is clearly needed here.
}
