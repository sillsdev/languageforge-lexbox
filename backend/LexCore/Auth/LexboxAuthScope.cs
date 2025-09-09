namespace LexCore.Auth;

public enum LexboxAuthScope
{
    //oauth scopes
    openid,
    profile,
    email,

    //app scopes
    LexboxApi,
    RegisterAccount,
    ForgotPassword,
    SendAndReceive,
    SendAndReceiveRefresh,
}
