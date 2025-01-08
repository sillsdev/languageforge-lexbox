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
    public void Fails_WhenLexemeFormHasWsWithEmptyContent()
    {
        var entry = new Entry() { Id = Guid.NewGuid(), LexemeForm = new MultiString(){{"en", ""}} };
        _validator.TestValidate(entry).ShouldHaveValidationErrorFor("LexemeForm");
    }

    [Theory]
    [InlineData(nameof(Entry.CitationForm))]
    [InlineData(nameof(Entry.LiteralMeaning))]
    [InlineData(nameof(Entry.Note))]
    public void Succeeds_WhenNonEmptyFieldIsPresent(string fieldName)
    {
        var entry = new Entry() { Id = Guid.NewGuid(), LexemeForm = new MultiString(){{"en", "lexeme"}} };
        SetProperty(entry, fieldName, "content");
        _validator.TestValidate(entry).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(nameof(Entry.CitationForm))]
    [InlineData(nameof(Entry.LiteralMeaning))]
    [InlineData(nameof(Entry.Note))]
    public void Succeeds_WhenNonEmptyFieldHasNoContent(string fieldName)
    {
        var entry = new Entry() { Id = Guid.NewGuid(), LexemeForm = new MultiString(){{"en", "lexeme"}} };
        MakePropertyEmpty(entry, fieldName);
        _validator.TestValidate(entry).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(nameof(Entry.CitationForm))]
    [InlineData(nameof(Entry.LiteralMeaning))]
    [InlineData(nameof(Entry.Note))]
    public void Fails_WhenNonEmptyFieldHasWsWithEmptyContent(string fieldName)
    {
        var entry = new Entry() { Id = Guid.NewGuid(), LexemeForm = new MultiString(){{"en", "lexeme"}} };
        SetProperty(entry, fieldName, "");
        _validator.TestValidate(entry).ShouldHaveValidationErrorFor(fieldName);
    }

    [Fact]
    public void Fails_WhenComplexFormsContainCircularReference()
    {
        var entryId = Guid.NewGuid();
        var entry = new Entry() { Id = entryId, LexemeForm = new MultiString(){{"en", "lexeme"}}, ComplexForms = [new ComplexFormComponent(){ ComplexFormEntryId = entryId, ComponentEntryId = Guid.Empty }] };
        _validator.TestValidate(entry).ShouldHaveValidationErrorFor("ComplexForms[0]");
    }

    [Fact]
    public void Fails_WhenComponentsContainCircularReference()
    {
        var entryId = Guid.NewGuid();
        var entry = new Entry() { Id = entryId, LexemeForm = new MultiString(){{"en", "lexeme"}}, Components = [new ComplexFormComponent(){ ComplexFormEntryId = Guid.Empty, ComponentEntryId = entryId }] };
        _validator.TestValidate(entry).ShouldHaveValidationErrorFor("Components[0]");
    }


    [Fact]
    public void Succeeds_WhenComplexFormsContainEmptyGuid()
    {
        var entryId = Guid.NewGuid();
        var entry = new Entry() { Id = entryId, LexemeForm = new MultiString(){{"en", "lexeme"}}, ComplexForms = [new ComplexFormComponent(){ ComplexFormEntryId = Guid.Empty, ComponentEntryId = entryId }] };
        _validator.TestValidate(entry).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Succeeds_WhenComponentsContainEmptyGuid()
    {
        var entryId = Guid.NewGuid();
        var entry = new Entry() { Id = entryId, LexemeForm = new MultiString(){{"en", "lexeme"}}, Components = [new ComplexFormComponent(){ ComplexFormEntryId = entryId, ComponentEntryId = Guid.Empty }] };
        _validator.TestValidate(entry).ShouldNotHaveAnyValidationErrors();
    }

    private void SetProperty(Entry entry, string propName, string content)
    {
        var propInfo = typeof(Entry).GetProperty(propName);
        propInfo?.SetValue(entry, new MultiString(){{"en", content}});
    }

    private void MakePropertyEmpty(Entry entry, string propName)
    {
        var propInfo = typeof(Entry).GetProperty(propName);
        propInfo?.SetValue(entry, new MultiString());
    }
}
