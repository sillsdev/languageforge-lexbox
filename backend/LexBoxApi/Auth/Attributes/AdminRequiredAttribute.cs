namespace LexBoxApi.Auth.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AdminRequiredAttribute : LexboxAuthAttribute
{
    public const string PolicyName = "AdminRequiredPolicy";

    public AdminRequiredAttribute() : base(PolicyName)
    {
    }
}


public static class AdminRequiredAttributeExtensions
{
    public static IObjectFieldDescriptor AdminRequired(this IObjectFieldDescriptor descriptor)
    {
        return descriptor.Authorize(AdminRequiredAttribute.PolicyName);
    }
}
