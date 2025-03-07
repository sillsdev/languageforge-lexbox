namespace LexCore.Auth;

public static class LexAuthConstants
{
    public const string AuthCookieName = ".LexBoxAuth";
    public const string RoleClaimType = "role";
    public const string EmailClaimType = "email";
    public const string UsernameClaimType = "user";
    public const string NameClaimType = "name";
    public const string IdClaimType = "sub";
    public const string AudienceClaimType = "aud";
    public const string ProjectsClaimType = "proj";
    public const string OrgsClaimType = "orgs";
    public const string FeatureFlagsClaimType = "feat";
    public const string IsLockedClaimType = "lock";
    public const string EmailUnverifiedClaimType = "unver";
    public const string CanCreateProjectClaimType = "mkproj";
    public const string CreatedByAdminClaimType = "creat";
    public const string UpdatedDateClaimType = "date";
    public const string LocaleClaimType = "loc";
    public const string ScopeClaimType = "scope";
    public const string JwtUpdatedHeader = "lexbox-jwt-updated";
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
