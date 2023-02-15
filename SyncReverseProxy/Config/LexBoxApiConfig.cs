using System.ComponentModel.DataAnnotations;

namespace WebApi.Config;

public class LexBoxApiConfig
{
    [Required]
    public required string Url { get; set; }
}