using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes;

public class EditPluginChange : EditChange<Plugin>, ISelfNamedType<EditPluginChange>
{
    [SetsRequiredMembers]
    public EditPluginChange(Guid entityId, Plugin plugin) : base(entityId)
    {
        Name = plugin.Name;
        Html = plugin.Html;
    }

    [JsonConstructor]
    private EditPluginChange(Guid entityId) : base(entityId)
    {
    }

    public required string Name { get; set; }
    public required string Html { get; set; }

    public override ValueTask ApplyChange(Plugin entity, IChangeContext context)
    {
        entity.Name = Name;
        entity.Html = Html;
        return ValueTask.CompletedTask;
    }
}
