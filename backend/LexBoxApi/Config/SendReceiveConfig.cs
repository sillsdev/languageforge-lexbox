using System.ComponentModel.DataAnnotations;

namespace LexBoxApi.Config;

public class SendReceiveConfig
{
    [Required]
    public required string FdoDataModelVersion { get; init; }
}
