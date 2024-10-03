using System.ComponentModel.DataAnnotations;

namespace LfClassicData;

public class LfClassicConfig
{
    [Required]
    public required string ConnectionString { get; set; }

    public string? AuthSource { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan ServerSelectionTimeout { get; set; } = TimeSpan.FromSeconds(5);
    /// <summary>
    /// how long to wait before trying to determine if a project is an LF project after a failure
    /// </summary>
    public TimeSpan IsLfProjectConnectionRetryTimeout { get; set; } = TimeSpan.FromSeconds(60);
    public bool HasCredentials => AuthSource is not null && Username is not null && Password is not null;
}
