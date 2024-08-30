namespace MiniLcm;

public class SemanticDomain : IObjectWithId
{
    public virtual required Guid Id { get; set; }
    public virtual required MultiString Name { get; set; }
    public virtual required string Code { get; set; }
}
