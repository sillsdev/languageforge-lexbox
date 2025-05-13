using FluentValidation;
using MiniLcm.Models;

namespace MiniLcm.Validators;

public class WritingSystemUpdateValidator : AbstractValidator<UpdateObjectInput<WritingSystem>>
{
    public WritingSystemUpdateValidator()
    {
        RuleFor(u => u.Patch).Custom((document, context) =>
        {
            //todo pull this out to make it easy to define properties that aren't allowed to be changed
            if (document.Operations.Any(o =>
                    string.Equals(o.Path,
                        $"/{nameof(WritingSystem.WsId)}",
                        StringComparison.InvariantCultureIgnoreCase)))
                context.AddFailure(nameof(WritingSystem.WsId), "Not allowed to update WsId");
        });
        RuleFor(u => u.Patch).NoOperation(nameof(WritingSystem.WsId));
        RuleFor(u => u.Patch).NoOperation(nameof(WritingSystem.Type));
        RuleFor(u => u.Patch).NoOperation(nameof(WritingSystem.DeletedAt));
        RuleFor(u => u.Patch.Operations).NotEmpty();
    }
}
