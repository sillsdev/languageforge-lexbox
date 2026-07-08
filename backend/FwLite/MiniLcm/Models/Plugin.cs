namespace MiniLcm.Models;

/// <summary>
/// A user-authored, project-scoped plugin: a self-contained HTML document that FW Lite runs
/// in a sandboxed iframe. Stored in the CRDT so it syncs to teammates; never synced to FwData.
/// </summary>
public record Plugin : IObjectWithId<Plugin>
{
    public Guid Id { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string Html { get; set; }

    public Guid[] GetReferences()
    {
        return [];
    }

    public void RemoveReference(Guid id, DateTimeOffset time)
    {
    }

    public Plugin Copy()
    {
        return this with { };
    }
}
