namespace MiniLcm.Models;

public interface IOrderableNoId
{
    double Order { get; set; }
}

public interface IOrderable: IOrderableNoId
{
    Guid Id { get; }
}
