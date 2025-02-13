namespace MiniLcm.Models;

public interface IOrderable
{
    Guid Id { get; }
    double Order { get; set; }
}
