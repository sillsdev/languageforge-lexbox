using System.ComponentModel.DataAnnotations;

namespace LexCore.Config;

public class HgConfig
{
    [Required]
    public required string RepoPath { get; init; }
    [Required]
    public required string SendReceiveDomain { get; init; }
    [Required, Url, RegularExpression(@"^.+/$", ErrorMessage = "Must end with '/'")]
    public required string HgWebUrl { get; init; }

    [Required, Url, RegularExpression(@"^.+/$", ErrorMessage = "Must end with '/'")]
    public required string HgCommandServer { get; init; }
    [Required, Url]
    public required string HgResumableUrl { get; init; }

    [Required, Url, RegularExpression(@"^.+/$", ErrorMessage = "Must end with '/'")]
    public required string PublicRedmineHgWebUrl { get; init; }
    [Required, Url, RegularExpression(@"^.+/$", ErrorMessage = "Must end with '/'")]
    public required string PrivateRedmineHgWebUrl { get; init; }

    [Required, Url]
    public required string RedmineHgResumableUrl { get; init; }

    [Required(AllowEmptyStrings = false)]
    public required string RedmineTrustToken { get; init; }
    public string MigrationHost { get; init; } = "sysops.languagedepot.org";
}
