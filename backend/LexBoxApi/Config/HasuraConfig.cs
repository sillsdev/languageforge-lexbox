using System.ComponentModel.DataAnnotations;

namespace LexBoxApi.Config;

public class HasuraConfig
{
    [Required]
    public required string HasuraUrl { get; init; }
    [Required]
    public required string HasuraSecret { get; init; }
}