using System.ComponentModel.DataAnnotations;

namespace LexBoxApi.Auth;

public class OpenIdOptions
{
    [Required]
    public required bool Enable { get; set; }
}
