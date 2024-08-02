namespace MiniLcm;

public class PartOfSpeech
{
    public Guid Id { get; set; }
    public MultiString Name { get; set; } = new();
}
