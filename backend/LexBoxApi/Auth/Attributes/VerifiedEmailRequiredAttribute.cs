namespace LexBoxApi.Auth.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class VerifiedEmailRequiredAttribute : LexboxAuthAttribute
{
    public const string PolicyName = "VerifiedEmailRequiredPolicy";

    public VerifiedEmailRequiredAttribute() : base(PolicyName)
    {
    }
}
