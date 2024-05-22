using System.ComponentModel.DataAnnotations;

namespace LexData.Configuration;

public class DbConfig
{
    [Required]
    public required string LexBoxConnectionString { get; set; }
    public string DefaultSeedUserPassword { get; init; } = "pass";
    public string QaAdminEmail { get; init; } = "qa@test.com";
}
