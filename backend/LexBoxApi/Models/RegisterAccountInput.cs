using System.ComponentModel.DataAnnotations;

namespace LexBoxApi.Models;

public record RegisterAccountInput([Required(AllowEmptyStrings = false)] string Name,
    [EmailAddress] string Email,
    [Required(AllowEmptyStrings = false)] string PasswordHash,
    [Required(AllowEmptyStrings = false)] string TurnstileToken);
