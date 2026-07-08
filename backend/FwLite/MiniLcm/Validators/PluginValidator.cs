using FluentValidation;
using MiniLcm.Models;

namespace MiniLcm.Validators;

public class PluginValidator : AbstractValidator<Plugin>
{
    // Caps the commit payload so a single plugin can't bloat sync for the whole project.
    public const int MaxHtmlLength = 5_000_000;
    public const int MaxDescriptionLength = 1_000;

    public PluginValidator()
    {
        RuleFor(p => p.DeletedAt).Null();
        RuleFor(p => p.Name).Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("Plugin name is required");
        RuleFor(p => p.Description).MaximumLength(MaxDescriptionLength).When(p => p.Description is not null);
        RuleFor(p => p.Html).Must(html => !string.IsNullOrWhiteSpace(html))
            .WithMessage("Plugin HTML is required")
            .MaximumLength(MaxHtmlLength);
    }
}
