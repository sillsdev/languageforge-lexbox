namespace MiniLcm;

public record CreateEntryOptions(
    /// <summary>
    /// Can be excluded for the purpose of deferring referencing entities that might not exist yet.
    /// </summary>
    bool IncludeComplexFormsAndComponents = true
)
{
    public static readonly CreateEntryOptions Everything = new();
    public static readonly CreateEntryOptions WithoutComplexFormsAndComponents
        = new(IncludeComplexFormsAndComponents: false);
}
