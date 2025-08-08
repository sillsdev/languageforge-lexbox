using MiniLcm.Models;

namespace MiniLcm.Wrappers;

public interface IMiniLcmWrapperFactory
{
    IMiniLcmApi Create(IMiniLcmApi api, IProjectIdentifier project);
}

public static class MiniLcmWrapperExtensions
{
    public static IMiniLcmApi WrapWith(this IMiniLcmApi api, IEnumerable<IMiniLcmWrapperFactory> factories, IProjectIdentifier project)
    {
        var wrappedApi = api;
        foreach (var factory in factories.Reverse())
        {
            wrappedApi = factory.Create(wrappedApi, project);
        }
        return wrappedApi;
    }
}
