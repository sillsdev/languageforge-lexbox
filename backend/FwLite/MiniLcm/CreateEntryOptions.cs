namespace MiniLcm;

public record CreateEntryOptions(
    /// <summary>
    /// Gates all cross-entry links (complex forms, components and variants). Can be excluded
    /// for the purpose of deferring referenced entities that might not exist yet.
    /// </summary>
    bool IncludeEntryReferences = true,
    bool AutoAddMainPublication = false
)
{
    /// <summary>Create the entry exactly as given — no main publication injected.</summary>
    public static readonly CreateEntryOptions AsIs = new();

    /// <summary>For interactive entry creation: auto-add the project's main publication.</summary>
    public static readonly CreateEntryOptions WithMainPublication = new(AutoAddMainPublication: true);

    /// <summary>Defer cross-entry links (complex forms, components, variants) to a later sync pass (they may reference entries that don't exist yet).</summary>
    public static readonly CreateEntryOptions WithoutEntryReferences = new(IncludeEntryReferences: false);
}
