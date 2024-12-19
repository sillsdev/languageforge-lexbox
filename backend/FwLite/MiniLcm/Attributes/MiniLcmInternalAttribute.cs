namespace MiniLcm.Attributes;
/// <summary>
/// For now this just controls whether the property should be serialized or not when leaving dotnet
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true)]
public class MiniLcmInternalAttribute : Attribute
{
}
