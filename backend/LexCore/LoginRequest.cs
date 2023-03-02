namespace LexCore;

public record LoginRequest(string Password, string EmailOrUsername, bool PreHashedPassword = false);