using System.ComponentModel.DataAnnotations;

namespace LexCore.Config;

public class MediaFileConfig
{
    [Required, Url, RegularExpression(@"^.+/$", ErrorMessage = "Must end with '/'")]
    public required string FwHeadlessUrl { get; init; }
}
