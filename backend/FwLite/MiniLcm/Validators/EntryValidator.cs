using FluentValidation;
using MiniLcm.Models;

namespace MiniLcm.Validators;

public class EntryValidator : AbstractValidator<Entry>
{
    public EntryValidator()
    {
        RuleForEach(e => e.Senses).SetValidator(entry => new SenseValidator(entry));
        //todo just a stub as an example for senses
    }
}
