using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using CrdtLib.Changes;
using CrdtLib.Db;
using CrdtLib.Entities;
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

    [SetsRequiredMembers]
    public CreateWritingSystemChange(MiniLcm.WritingSystem writingSystem, WritingSystemType type, Guid entityId) :
        base(entityId)
    {
        WsId = writingSystem.Id;
        Name = writingSystem.Name;
        Abbreviation = writingSystem.Abbreviation;
        Font = writingSystem.Font;
        Exemplars = writingSystem.Exemplars;
        Type = type;
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
            Type = Type
        };
    }

    public override ValueTask ApplyChange(WritingSystem entity, ChangeContext context)
    {
        entity.Name = Name;
        entity.Abbreviation = Abbreviation;
        entity.Font = Font;
        entity.Exemplars = Exemplars;
        entity.Type = Type;
        return ValueTask.CompletedTask;
    }
}
