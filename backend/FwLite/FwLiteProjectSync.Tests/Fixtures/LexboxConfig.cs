using System.ComponentModel.DataAnnotations;

namespace FwLiteProjectSync.Tests.Fixtures;

public class LexboxConfig
{
    [Required, Url, RegularExpression(@"^.+/$", ErrorMessage = "Must end with '/'")]
    public required string LexboxUrl { get; set; }
    public string HgWebUrl => $"{LexboxUrl}hg/";
    [Required]
    public required string LexboxUsername { get; set; }
    [Required]
    public required string LexboxPassword { get; set; }
}
