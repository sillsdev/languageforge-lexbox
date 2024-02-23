using System.Text.Json.Serialization;

namespace LexCore.Auth;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LexboxAudience
{
    //default value of the enum, not a valid audience
    Unknown,
    //these names are converted to strings and are used in jwt tokens, if the name is changed that will invalidate all existing tokens
    LexboxApi,
    RegisterAccount,
    ForgotPassword,
    SendAndReceive,
    SendAndReceiveRefresh,
}
