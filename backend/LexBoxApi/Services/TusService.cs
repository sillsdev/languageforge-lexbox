using LexBoxApi.Config;
using Microsoft.Extensions.Options;
using tusdotnet.Models;
using tusdotnet.Models.Configuration;
using tusdotnet.Stores;
using Path = System.IO.Path;

namespace LexBoxApi.Services;

public class TusService
{
    private readonly TusConfig _config;

    public TusService(IOptions<TusConfig> config)
    {
        _config = config.Value;
        Directory.CreateDirectory(Path.GetFullPath(_config.TestUploadPath));
    }

    public async Task<DefaultTusConfiguration> GetTestConfig(HttpContext context)
    {
        return new DefaultTusConfiguration
        {
            Store = new TusDiskStore(Path.GetFullPath(_config.TestUploadPath)),
            Events = new Events
            {
                OnBeforeCreateAsync = createContext =>
                {
                    //todo only allow images to be uploaded
                    return Task.CompletedTask;
                },
                OnFileCompleteAsync = async completeContext =>
                {

                }
            }
        };
    }
}
