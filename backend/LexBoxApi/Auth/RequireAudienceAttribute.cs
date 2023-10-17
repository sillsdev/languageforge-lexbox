namespace LexBoxApi.Auth;

public class RequireAudienceAttribute : LexboxAuthAttribute
{
    public RequireAudienceAttribute(LexboxAudience audience) : base(audience.PolicyName())
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
