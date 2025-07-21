using Refit;

namespace LcmCrdt.RemoteSync;

public interface IRefitHttpServiceFactory
{
    T Service<T>(HttpClient client);
}

public class RefitHttpServiceFactory(RefitSettings refit) : IRefitHttpServiceFactory
{
    public T Service<T>(HttpClient client)
    {
        return RestService.For<T>(client, refit);
    }
}
