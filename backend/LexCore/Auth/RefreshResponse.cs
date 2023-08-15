namespace LexCore.Auth;

/// <summary>
/// this record represents a contract between FLEx and lexbox, take that into consideration before making any changes
/// </summary>
/// <param name="ProjectToken"></param>
/// <param name="ProjectTokenExpiresAt"></param>
/// <param name="FlexToken"></param>
/// <param name="FlexTokenExpiresAt"></param>
public record RefreshResponse(
    string ProjectToken,
    DateTime ProjectTokenExpiresAt,
    string FlexToken,
    DateTime FlexTokenExpiresAt);
