using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using MiniLcm.Media;
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
        Description = plugin.Description;
        FileUri = plugin.FileUri;
        FileSize = plugin.FileSize;
        Permissions = plugin.Permissions;
        Contexts = plugin.Contexts;
        Requires = plugin.Requires;
    }

    [JsonConstructor]
    private EditPluginChange(Guid entityId) : base(entityId)
    {
    }

    public required string Name { get; set; }
    public string? Description { get; set; }
    public required MediaUri FileUri { get; set; }
    public long FileSize { get; set; }
    public string[] Permissions { get; set; } = [];
    public string[] Contexts { get; set; } = [];
    public string[] Requires { get; set; } = [];

    public override ValueTask ApplyChange(Plugin entity, IChangeContext context)
    {
        entity.Name = Name;
        entity.Description = Description;
        entity.FileUri = FileUri;
        entity.FileSize = FileSize;
        entity.Permissions = Permissions;
        entity.Contexts = Contexts;
        entity.Requires = Requires;
        return ValueTask.CompletedTask;
    }
}
