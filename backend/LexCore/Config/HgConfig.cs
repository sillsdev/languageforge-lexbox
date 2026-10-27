using System.ComponentModel.DataAnnotations;

namespace LexCore.Config;

public class HgConfig
{
    [Required]
    public required string SendReceiveDomain { get; init; }
    [Required, Url, RegularExpression(@"^.+/$", ErrorMessage = "Must end with '/'")]
    public required string HgWebUrl { get; init; }

    [Required, Url, RegularExpression(@"^.+/$", ErrorMessage = "Must end with '/'")]
    public required string HgCommandServer { get; init; }
    [Required, Url]
    public required string HgResumableUrl { get; init; }
    /// <summary>
    /// Sent as X-Manage-Secret to hgresume's /api/manage, which does every repo filesystem operation for lexbox
    /// </summary>
    [Required]
    public required string HgResumeManageSecret { get; init; }
    public bool AutoUpdateLexEntryCountOnSendReceive { get; init; } = false;
    public bool RequireContainerVersionMatch { get; init; } = true;
    public int ResetCleanupAgeDays { get; init; } = 31;
}
