namespace LexCore.ServiceInterfaces;

public interface IIsHarmonyProjectDataLoader
{
    Task<bool> LoadAsync(Guid projectId, CancellationToken cancellationToken = default);
}
