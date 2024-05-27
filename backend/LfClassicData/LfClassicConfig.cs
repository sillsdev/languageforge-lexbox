using System.ComponentModel.DataAnnotations;

namespace LfClassicData;

public class LfClassicConfig
{
    [Required]
    public required string ConnectionString { get; set; }

    public string? AuthSource { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool HasCredentials => AuthSource is not null && Username is not null && Password is not null;
}
