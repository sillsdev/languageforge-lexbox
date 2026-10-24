using MiniLcm.Media;

namespace MiniLcm.Models;

/// <summary>
/// A user-authored, project-scoped plugin: a self-contained HTML document that FW Lite runs
/// in a sandboxed iframe. This entity carries only the metadata; the HTML itself is stored as a
/// media resource (see <see cref="FileUri"/>), so the bytes travel the same path as audio and
/// pictures — including Mercurial send/receive, where FieldWorks sees them as ordinary linked files.
/// </summary>
public record Plugin : IObjectWithId<Plugin>
{
    public Guid Id { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public required string Name { get; set; }
    public string? Description { get; set; }

    /// <summary>
    /// The plugin's HTML document, stored as a media resource. Plugin files are immutable: editing
    /// a plugin saves a new file and repoints this reference, so downloaded copies never go stale.
    /// </summary>
    public required MediaUri FileUri { get; set; }

    /// <summary>Size of the HTML file in bytes, so lists can show it without fetching the file.</summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Manifest tokens declared in the HTML's <c>&lt;meta name="fwlite-plugin-*"&gt;</c> tags
    /// (permissions like "internet"/"edit", launch contexts like "entry", required host features
    /// like "comments"/"history"), extracted when the plugin is saved. They let clients list and
    /// place plugins without fetching the file; enforcement always re-reads the actual HTML.
    /// </summary>
    public string[] Permissions { get; set; } = [];
    public string[] Contexts { get; set; } = [];
    public string[] Requires { get; set; } = [];

    public Guid[] GetReferences()
    {
        return [];
    }

    public void RemoveReference(Guid id, DateTimeOffset time)
    {
    }

    public Plugin Copy()
    {
        return this with
        {
            Permissions = [.. Permissions],
            Contexts = [.. Contexts],
            Requires = [.. Requires],
        };
    }
}
