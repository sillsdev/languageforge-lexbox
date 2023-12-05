namespace LexCore.Auth;

public static class LexAuthConstants
{
    public const string RoleClaimType = "role";
    public const string EmailClaimType = "email";
    public const string NameClaimType = "name";
    public const string IdClaimType = "sub";
    public const string AudienceClaimType = "aud";
    public const string ProjectsClaimType = "proj";
    public const string EmailUnverifiedClaimType = "unver";
    public const string CanCreateProjectClaimType = "mkproj";
    public const string UpdatedDateClaimType = "date";
}

/// <summary>
/// Constants for Json Web tokens. Copied from Microsoft.IdentityModel.JsonWebTokens
/// </summary>
public static class JsonClaimValueTypes
{
    public const string Json = "JSON";
    public const string JsonArray = "JSON_ARRAY";
    public const string JsonNull = "JSON_NULL";
}
