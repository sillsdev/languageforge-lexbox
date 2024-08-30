using SIL.Harmony;
using SIL.Harmony.Changes;
using SIL.Harmony.Db;
using SIL.Harmony.Entities;
using MiniLcm.Models;

namespace LcmCrdt.Objects;

public class WritingSystem : IObjectBase<WritingSystem>, IOrderableCrdt
{
    public WritingSystem()
    {
    }

    public WritingSystem(Guid id)
    {
        Id = id;
    }

    public Guid Id { get; init; }
    public DateTimeOffset? DeletedAt { get; set; }
    public required WritingSystemId WsId { get; init; }
    public required WritingSystemType Type { get; set; }
    public required string Name { get; set; }
    public required string Abbreviation { get; set; }
    //todo need to accommodate font features too
    public required string Font { get; set; }

    public string[] Exemplars { get; set; } = [];
    public double Order { get; set; }

    public Guid[] GetReferences()
    {
        return [];
    }

    public void RemoveReference(Guid id, Commit commit)
    {
    }

    public IObjectBase Copy()
    {
        return new WritingSystem(Id)
        {
            WsId = WsId,
            Name = Name,
            Abbreviation = Abbreviation,
            Font = Font,
            Exemplars = Exemplars,
            DeletedAt = DeletedAt,
            Type = Type,
            Order = Order
        };
    }


    public static implicit operator MiniLcm.Models.WritingSystem(WritingSystem ws) =>
        new()
        {
            Id = ws.WsId,
            Name = ws.Name,
            Abbreviation = ws.Abbreviation,
            Font = ws.Font,
            Exemplars = ws.Exemplars
        };
}
