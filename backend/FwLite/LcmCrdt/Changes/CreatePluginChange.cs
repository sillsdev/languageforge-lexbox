using System.Text.Json.Serialization;
using SIL.Harmony;
using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes;

public class CreatePluginChange : CreateChange<Plugin>, ISelfNamedType<CreatePluginChange>
{
    public CreatePluginChange(Guid entityId, Plugin plugin) : base(entityId)
    {
        Name = plugin.Name;
        Html = plugin.Html;
    }

    [JsonConstructor]
    public CreatePluginChange(Guid entityId) : base(entityId)
    {
    }

    public string Name { get; set; } = string.Empty;
    public string Html { get; set; } = string.Empty;

    public override ValueTask<Plugin> NewEntity(Commit commit, IChangeContext context)
    {
        return ValueTask.FromResult(new Plugin
        {
            Id = EntityId,
            Name = Name,
            Html = Html
        });
    }
}
