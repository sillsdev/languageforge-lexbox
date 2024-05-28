namespace LexCore.ServiceInterfaces;

public interface IIsLanguageForgeProjectDataLoader
{
    public Task<bool> LoadAsync(string projectCode, CancellationToken cancellationToken = default);
}
