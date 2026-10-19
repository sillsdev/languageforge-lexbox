namespace LexboxAnalyzers;

/// <summary>
/// Central allocation of diagnostic IDs. IDs use the <c>LX</c> prefix followed by four digits
/// and are <b>never reused or renumbered</b> once shipped — code suppressions and
/// <c>.editorconfig</c> severities reference them by string.
/// </summary>
internal static class DiagnosticIds
{
    /// <summary>CRDT change types must declare a <c>Guid entityId</c> constructor.</summary>
    public const string ChangeMustHaveEntityIdConstructor = "LX0001";
}
