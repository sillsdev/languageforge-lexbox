namespace LexBoxApi.Auth;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AdminRequiredAttribute : LexboxAuthAttribute
{
    public const string PolicyName = "AdminRequiredPolicy";

    public AdminRequiredAttribute() : base(PolicyName)
    {
    }
}
