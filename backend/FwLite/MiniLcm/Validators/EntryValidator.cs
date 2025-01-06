using FluentValidation;
using MiniLcm.Models;

namespace MiniLcm.Validators;

public class EntryValidator : AbstractValidator<Entry>
{
    public EntryValidator()
    {
        RuleFor(e => e.DeletedAt).Null();
        RuleFor(e => e.LexemeForm).Required();
        RuleFor(e => e.CitationForm).NoEmptyValues();
        RuleFor(e => e.LiteralMeaning).NoEmptyValues();
        RuleFor(e => e.Note).NoEmptyValues();
        RuleForEach(e => e.Senses).SetValidator(entry => new SenseValidator(entry));
        RuleForEach(e => e.Components).Must(NotBeComponentSelfReference);
        RuleForEach(e => e.Components).Must(HaveCorrectComponentEntryReference);
        RuleForEach(e => e.ComplexForms).Must(NotBeComplexFormSelfReference);
        RuleForEach(e => e.ComplexForms).Must(HaveCorrectComplexFormEntryReference);
        // RuleForEach(e => e.Components).SetValidator(entry => new ComplexFormComponentValidator(entry)); // TODO: Not implemented yet
        // RuleForEach(e => e.ComplexForms).SetValidator(entry => new ComplexFormComponentValidator(entry)); // TODO: Not implemented yet
        // TODO: ComplexFormComponentValidator(entry) might need to know the "direction" of the entry it's validating, i.e. one class for "I'm a component" and another for "I'm the complex entry"
        RuleForEach(e => e.ComplexFormTypes).SetValidator(new ComplexFormTypeValidator());
    }

    private bool NotBeComponentSelfReference(Entry entry, ComplexFormComponent component)
    {
        return component.ComponentEntryId != entry.Id;
    }

    private bool HaveCorrectComponentEntryReference(Entry entry, ComplexFormComponent component)
    {
        return component.ComplexFormEntryId == entry.Id;
    }

    private bool NotBeComplexFormSelfReference(Entry entry, ComplexFormComponent component)
    {
        return component.ComplexFormEntryId != entry.Id;
    }

    private bool HaveCorrectComplexFormEntryReference(Entry entry, ComplexFormComponent component)
    {
        return component.ComponentEntryId == entry.Id;
    }
}
