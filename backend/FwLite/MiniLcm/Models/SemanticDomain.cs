namespace MiniLcm.Models;

public class SemanticDomain : IPossibility, IObjectWithId<SemanticDomain>
{
    public virtual Guid Id { get; set; }
    public virtual MultiString Name { get; set; } = new();
    public virtual MultiString Abbreviation { get; set; } = new();
    /// <summary>
    /// Convenience code for UI/filters. Prefer Abbreviation for multi-writing-system data.
    /// When empty, filled from Abbreviation via <see cref="ApplyResolvedCode"/>.
    /// When set (e.g. by FLEx), Code is preferred over Abbreviation.
    /// </summary>
    public virtual string Code { get; set; } = string.Empty;
    public virtual RichMultiString Description { get; set; } = new();
    public virtual string? OcmCodes { get; set; }
    public virtual string? LouwNidaCodes { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public bool Predefined { get; set; }

    /// <summary>First non-empty Abbreviation value, preferring English.</summary>
    public static string CodeFromAbbreviation(MultiString abbreviation)
    {
        if (abbreviation.Values.TryGetValue("en", out var en) && !string.IsNullOrEmpty(en))
            return en;
        foreach (var value in abbreviation.Values.Values)
        {
            if (!string.IsNullOrEmpty(value))
                return value;
        }
        return string.Empty;
    }

    /// <summary>
    /// Prefer a non-empty <paramref name="code"/> (e.g. FLEx-supplied); otherwise derive from Abbreviation.
    /// Never invents Abbreviation from Code.
    /// </summary>
    public static string ResolveCode(MultiString abbreviation, string? code)
    {
        if (!string.IsNullOrEmpty(code))
            return code;
        return CodeFromAbbreviation(abbreviation);
    }

    /// <summary>Fill <see cref="Code"/> from Abbreviation only when Code is empty.</summary>
    public void ApplyResolvedCode()
    {
        Code = ResolveCode(Abbreviation, Code);
    }

    public Guid[] GetReferences()
    {
        return [];
    }

    public void RemoveReference(Guid id, DateTimeOffset time)
    {
    }

    public SemanticDomain Copy()
    {
        return new SemanticDomain
        {
            Id = Id,
            Code = Code,
            Name = Name.Copy(),
            Abbreviation = Abbreviation.Copy(),
            Description = Description.Copy(),
            OcmCodes = OcmCodes,
            LouwNidaCodes = LouwNidaCodes,
            DeletedAt = DeletedAt,
            Predefined = Predefined
        };
    }
}
