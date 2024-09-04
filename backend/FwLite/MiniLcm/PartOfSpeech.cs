namespace MiniLcm;

public class PartOfSpeech : IObjectWithId
{
    public Guid Id { get; set; }
    public MultiString Name { get; set; } = new();
}
