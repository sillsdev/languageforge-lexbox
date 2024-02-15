using System.Text.Json.Serialization;
using LexCore.Utils;

namespace LexCore.Auth;

[JsonConverter(typeof(JsonSnakeCaseUpperStringEnumConverter))]
public enum LexboxAudience
{
    //default value of the enum, not a valid audience
    Unknown,
    //these names are converted to strings and are used in jwt tokens, if the name is changed that will invalidate all existing tokens
    LexboxApi,
    ForgotPassword,
    SendAndReceive,
    SendAndReceiveRefresh,
    Locked,
}
