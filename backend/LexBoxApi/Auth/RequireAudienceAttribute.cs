using LexCore.Auth;

namespace LexBoxApi.Auth;

//todo for now this attribute only supports a single audience, this may be fine for now.
//once we are at dotnet 8 we can support multiple audiences using the new IAuthorizationRequirementData api
//https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-8.0?view=aspnetcore-7.0#iauthorizationrequirementdata
public class RequireAudienceAttribute : LexboxAuthAttribute
{
    /// <param name="audience">audience allowed to access this endpoint</param>
    /// <param name="exclusive">when false the default audience is also allowed, when true the default audience is not allowed</param>
    public RequireAudienceAttribute(LexboxAudience audience, bool exclusive = false) : base(audience.PolicyName(exclusive))
    {
    }
}

public  class AllowAnyAudienceAttribute : LexboxAuthAttribute
{
    public const string PolicyName = "AllowAnyAudiencePolicy";
    public AllowAnyAudienceAttribute() : base(PolicyName)
    {
    }
}
