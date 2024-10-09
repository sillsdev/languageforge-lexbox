namespace MiniLcm.Models;

public class ExampleSentence : IObjectWithId
{
    public virtual Guid Id { get; set; }
    public virtual MultiString Sentence { get; set; } = new();
    public virtual MultiString Translation { get; set; } = new();
    public virtual string? Reference { get; set; } = null;
}
