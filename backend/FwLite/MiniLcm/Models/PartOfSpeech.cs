namespace MiniLcm.Models;

public class PartOfSpeech : IObjectWithId
{
    public Guid Id { get; set; }
    public MultiString Name { get; set; } = new();
}
