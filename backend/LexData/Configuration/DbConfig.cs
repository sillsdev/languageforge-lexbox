using System.ComponentModel.DataAnnotations;

namespace LexData.Configuration;

public class DbConfig
{
    [Required]
    public required string LexBoxConnectionString { get; init; }
    public string? RedmineConnectionString { get; init; }
}