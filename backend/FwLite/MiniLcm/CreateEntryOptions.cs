namespace MiniLcm;

public record CreateEntryOptions(
    /// <summary>
    /// Can be excluded for the purpose of deferring referencing entities that might not exist yet.
    /// </summary>
    bool IncludeComplexFormsAndComponents = true,
    bool AutoAddMainPublication = false
)
{
    /// <summary>Create the entry exactly as given — no main publication injected.</summary>
    public static readonly CreateEntryOptions AsIs = new();

    /// <summary>For interactive entry creation: auto-add the project's main publication.</summary>
    public static readonly CreateEntryOptions WithMainPublication = new(AutoAddMainPublication: true);

    /// <summary>Defer complex forms and components to a later sync pass (they may reference entries that don't exist yet).</summary>
    public static readonly CreateEntryOptions WithoutComplexFormsAndComponents = new(IncludeComplexFormsAndComponents: false);
}
