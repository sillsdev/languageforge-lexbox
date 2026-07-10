using FluentValidation;
using MiniLcm.Media;
using MiniLcm.Models;

namespace MiniLcm.Validators;

public class PluginValidator : AbstractValidator<Plugin>
{
    public const int MaxNameLength = 200;
    public const int MaxDescriptionLength = 1_000;
    // Manifest tokens come from a small known vocabulary; these caps just keep garbage out of sync.
    public const int MaxTokens = 20;
    public const int MaxTokenLength = 50;

    public PluginValidator()
    {
        RuleFor(p => p.DeletedAt).Null();
        RuleFor(p => p.Name).Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("Plugin name is required")
            .MaximumLength(MaxNameLength);
        RuleFor(p => p.Description).MaximumLength(MaxDescriptionLength).When(p => p.Description is not null);
        RuleFor(p => p.FileUri).Must(uri => uri != MediaUri.NotFound && uri.FileId != Guid.Empty)
            .WithMessage("Plugin file is required");
        RuleFor(p => p.FileSize).GreaterThanOrEqualTo(0).LessThanOrEqualTo(MediaFile.MaxFileSize);
        RuleFor(p => p.Permissions).Must(BeValidTokenList).WithMessage("Invalid permissions list");
        RuleFor(p => p.Contexts).Must(BeValidTokenList).WithMessage("Invalid contexts list");
        RuleFor(p => p.Requires).Must(BeValidTokenList).WithMessage("Invalid requires list");
    }

    private static bool BeValidTokenList(string[] tokens)
    {
        return tokens.Length <= MaxTokens
            && tokens.All(token => !string.IsNullOrWhiteSpace(token) && token.Length <= MaxTokenLength);
    }
}
