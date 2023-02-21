using System.ComponentModel.DataAnnotations;

namespace LexSyncReverseProxy.Config;

public class LexBoxApiConfig
{
    [Required]
    public required string Url { get; set; }
}