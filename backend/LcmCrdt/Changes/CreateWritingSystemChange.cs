using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Crdt;
using Crdt.Changes;
using Crdt.Db;
using Crdt.Entities;
using MiniLcm;

namespace LcmCrdt.Changes;

public class CreateWritingSystemChange : Change<WritingSystem>, ISelfNamedType<CreateWritingSystemChange>
{
    public required WritingSystemId WsId { get; init; }
    public required string Name { get; init; }
    public required string Abbreviation { get; init; }
    public required string Font { get; init; }
    public required string[] Exemplars { get; init; } = [];
    public required WritingSystemType Type { get; init; }
    public required double Order { get; init; }

    [SetsRequiredMembers]
    public CreateWritingSystemChange(MiniLcm.WritingSystem writingSystem, WritingSystemType type, Guid entityId, double order) :
        base(entityId)
    {
        WsId = writingSystem.Id;
        Name = writingSystem.Name;
        Abbreviation = writingSystem.Abbreviation;
        Font = writingSystem.Font;
        Exemplars = writingSystem.Exemplars;
        Type = type;
        Order = order;
    }

    [JsonConstructor]
    private CreateWritingSystemChange(Guid entityId) : base(entityId)
    {
    }

    public override IObjectBase NewEntity(Commit commit)
    {
        return new WritingSystem(EntityId)
        {
            WsId = WsId,
            Name = Name,
            Abbreviation = Abbreviation,
            Font = Font,
            Exemplars = Exemplars,
            Type = Type,
            Order = Order
        };
    }

    public override ValueTask ApplyChange(WritingSystem entity, ChangeContext context)
    {
        entity.Name = Name;
        entity.Abbreviation = Abbreviation;
        entity.Font = Font;
        entity.Exemplars = Exemplars;
        entity.Type = Type;
        entity.Order = Order;
        return ValueTask.CompletedTask;
    }
}
