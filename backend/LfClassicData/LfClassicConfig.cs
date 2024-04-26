using System.ComponentModel.DataAnnotations;

namespace LfClassicData;

public class LfClassicConfig
{
    [Required]
    public required string ConnectionString { get; set; }
}
