namespace MiniLcm.Models;

public interface IOrderable
{
    public Guid Id { get; }
    public double Order { get; set; }
}
