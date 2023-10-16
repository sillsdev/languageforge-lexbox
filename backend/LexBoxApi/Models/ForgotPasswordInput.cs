using System.ComponentModel.DataAnnotations;

namespace LexBoxApi.Models;

public record ForgotPasswordInput(
    [EmailAddress] string Email,
    string TurnstileToken);
