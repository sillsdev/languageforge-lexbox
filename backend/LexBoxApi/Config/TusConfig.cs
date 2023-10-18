using System.ComponentModel.DataAnnotations;

namespace LexBoxApi.Config;

public class TusConfig
{
    [Required(AllowEmptyStrings = false)]
    public required string TestUploadPath { get; set; }
    [Required(AllowEmptyStrings = false)]
    public required string ResetUploadPath { get; set; }
}
