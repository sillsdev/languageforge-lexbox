namespace MiniLcm;

public class ExampleSentence
{
    public virtual Guid Id { get; set; }
    public virtual MultiString Sentence { get; set; } = new();
    public virtual MultiString Translation { get; set; } = new();
    public virtual string? Reference { get; set; } = null;
}
