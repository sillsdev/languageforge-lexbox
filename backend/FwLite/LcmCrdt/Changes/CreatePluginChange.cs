using System.Text.Json.Serialization;
using MiniLcm.Media;
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
        Description = plugin.Description;
        FileUri = plugin.FileUri;
        FileSize = plugin.FileSize;
        Permissions = plugin.Permissions;
        Contexts = plugin.Contexts;
        Requires = plugin.Requires;
    }

    [JsonConstructor]
    public CreatePluginChange(Guid entityId) : base(entityId)
    {
    }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public MediaUri FileUri { get; set; } = MediaUri.NotFound;
    public long FileSize { get; set; }
    public string[] Permissions { get; set; } = [];
    public string[] Contexts { get; set; } = [];
    public string[] Requires { get; set; } = [];

    public override ValueTask<Plugin> NewEntity(Commit commit, IChangeContext context)
    {
        return ValueTask.FromResult(new Plugin
        {
            Id = EntityId,
            Name = Name,
            Description = Description,
            FileUri = FileUri,
            FileSize = FileSize,
            Permissions = Permissions,
            Contexts = Contexts,
            Requires = Requires
        });
    }
}
