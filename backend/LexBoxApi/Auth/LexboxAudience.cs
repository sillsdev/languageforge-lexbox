using System.Text.Json.Serialization;

namespace LexBoxApi.Auth;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LexboxAudience
{
    //these names are converted to strings and are used in jwt tokens, if the name is changed that will invalidate all existing tokens
    LexboxApi,
    ForgotPassword,
}

public static class LexboxAudienceHelper
{
    public static string PolicyName(this LexboxAudience audience) => $"RequireAudience{audience}Policy";
}
