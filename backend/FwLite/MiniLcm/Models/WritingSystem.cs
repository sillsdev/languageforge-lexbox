namespace MiniLcm.Models;

public record WritingSystem: IObjectWithId
{
    public required Guid Id { get; set; }
    public required WritingSystemId WsId { get; set; }
    public required string Name { get; set; }
    public required string Abbreviation { get; set; }
    public required string Font { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
    public required WritingSystemType Type { get; set; }
    public string[] Exemplars { get; set; } = [];
    //todo probably need more stuff here, see wesay for ideas

    public static string[] LatinExemplars => Enumerable.Range('A', 'Z' - 'A' + 1).Select(c => ((char)c).ToString()).ToArray();

    public double Order { get; set; }

    public Guid[] GetReferences()
    {
        return [];
    }

    public void RemoveReference(Guid id, DateTimeOffset time)
    {
    }

    public IObjectWithId Copy()
    {
        return new WritingSystem
        {
            Id = Id,
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
}

public record WritingSystems
{
    public WritingSystem[] Analysis { get; set; } = [];
    public WritingSystem[] Vernacular { get; set; } = [];
}

public enum WritingSystemType
{
    Vernacular = 0,
    Analysis = 1
}
