namespace LexCore;

public record LoginRequest(string Password, string EmailOrUsername, bool PreHashedPassword = false, int? PasswordStrength = null);
public enum LoginError
{
    BadCredentials,
    Locked,
}
