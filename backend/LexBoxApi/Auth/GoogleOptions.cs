using System.ComponentModel.DataAnnotations;

namespace LexBoxApi.Auth;

public class GoogleOptions
{
    [Required]
    public required string ClientSecret { get; init; }

    [Required]
    public required string ClientId { get; init; }
}
