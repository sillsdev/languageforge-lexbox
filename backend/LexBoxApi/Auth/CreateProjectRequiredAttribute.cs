namespace LexBoxApi.Auth;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class CreateProjectRequiredAttribute: LexboxAuthAttribute
{
    public const string PolicyName = "CreateProjectRequiredPolicy";
    public CreateProjectRequiredAttribute() : base(PolicyName)
    {
    }
}
