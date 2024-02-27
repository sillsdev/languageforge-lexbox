namespace LexCore;

public record LoginRequest(string Password, string EmailOrUsername, bool PreHashedPassword = false, int PasswordStrength = -1);
public enum LoginError
{
    BadCredentials,
    Locked,
}
