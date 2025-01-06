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
}
