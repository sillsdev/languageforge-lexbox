namespace LexCore;

public record LoginRequest(string Password, string EmailOrUsername, bool PreHashedPassword = false);
public enum LoginError
{
    BadCredentials,
    Locked,
}
