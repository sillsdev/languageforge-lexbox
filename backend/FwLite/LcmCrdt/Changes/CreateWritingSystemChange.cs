using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using LcmCrdt.Utils;
using SIL.Harmony;
using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes;

public class CreateWritingSystemChange : CreateChange<WritingSystem>, ISelfNamedType<CreateWritingSystemChange>
{
    public required WritingSystemId WsId { get; init; }
    public required string Name { get; init; }
    public required string Abbreviation { get; init; }
    public required string Font { get; init; }
    public required string[] Exemplars { get; init; } = [];
    public required WritingSystemType Type { get; init; }
    public required double Order { get; init; }
    // not required so changes serialized before this property existed still deserialize (false = enabled)
    public bool IsDisabled { get; init; }

    [SetsRequiredMembers]
    public CreateWritingSystemChange(WritingSystem writingSystem, Guid entityId, double order) :
        base(entityId)
    {
        WsId = writingSystem.WsId;
        Name = writingSystem.Name;
        Abbreviation = writingSystem.Abbreviation;
        Font = writingSystem.Font;
        Exemplars = writingSystem.Exemplars;
        Type = writingSystem.Type;
        Order = order;
        IsDisabled = writingSystem.IsDisabled;
    }

    [JsonConstructor]
    private CreateWritingSystemChange(Guid entityId) : base(entityId)
    {
    }

    public override async ValueTask<WritingSystem> NewEntity(Commit commit, IChangeContext context)
    {
        var alreadyExists = await context.GetObjectsOfType<WritingSystem>().AnyAsync(ws => ws.WsId == WsId && ws.Type == Type);
        return new WritingSystem
        {
            Id = EntityId,
            WsId = WsId,
            Name = Name,
            Abbreviation = Abbreviation,
            Font = Font,
            Exemplars = Exemplars,
            Type = Type,
            Order = Order,
            IsDisabled = IsDisabled,
            DeletedAt = alreadyExists ? commit.DateTime : null
        };
    }
}
