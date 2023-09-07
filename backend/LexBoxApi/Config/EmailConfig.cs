using System.ComponentModel.DataAnnotations;

namespace LexBoxApi.Config;

public class EmailConfig
{
    [Required]
    public required int SmtpPort { get; init; }
    [Required]
    public required string SmtpUser { get; init; }
    [Required]
    public required string SmtpPassword { get; init; }
    [Required]
    public required string SmtpHost { get; init; }
    [Required]
    public required string From { get; init; }

    [Required]
    public required string EmailRenderHost { get; init; }

    [Required]
    public required string CreateProjectEmailDestination { get; init; }
}
