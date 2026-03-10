using MiniLcm.Models;

namespace MiniLcm.Wrappers;

public interface IMiniLcmWrapperFactory
{
    IMiniLcmApi Create(IMiniLcmApi api, IProjectIdentifier project);
}

public static class MiniLcmWrapperExtensions
{
    /// <summary>
    /// Wraps <paramref name="api"/> with <paramref name="factories"/> in runtime call order
    /// (outermost to innermost). The first factory becomes the outermost wrapper.
    /// </summary>
    public static IMiniLcmApi WrapWith(this IMiniLcmApi api, IEnumerable<IMiniLcmWrapperFactory?> factories, IProjectIdentifier project)
    {
        var wrappedApi = api;
        foreach (var factory in factories.Reverse().Where(f => f != null).Cast<IMiniLcmWrapperFactory>())
        {
            wrappedApi = factory.Create(wrappedApi, project);
        }
        return wrappedApi;
    }
}
