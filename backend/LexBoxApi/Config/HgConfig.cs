using System.ComponentModel.DataAnnotations;

namespace LexBoxApi.Config;

public class HgConfig
{
    [Required]
    public required string RepoPath { get; init; }
}