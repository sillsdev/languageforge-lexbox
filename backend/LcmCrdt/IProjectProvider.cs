namespace LcmCrdt;

public interface IProjectProvider<TIdentifier>
{
    ValueTask<CrdtProject> GetProject(TIdentifier id);
}
