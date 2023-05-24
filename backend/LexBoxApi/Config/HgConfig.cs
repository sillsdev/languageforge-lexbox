using System.ComponentModel.DataAnnotations;

namespace LexBoxApi.Config;

public class HgConfig
{
    [Required]
    public required string RepoPath { get; init; }
    [Required]
    public required string HgWebUrl { get; init; }
}