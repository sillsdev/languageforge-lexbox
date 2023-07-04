using Microsoft.Extensions.Options;
using Yarp.ReverseProxy.Configuration;

namespace LexSyncReverseProxy.Services;

public class ProxyConfigValidationHostedService : IHostedService
{
    private readonly IConfigValidator _validator;
    private readonly IProxyConfigProvider _proxyConfigProvider;

    public ProxyConfigValidationHostedService(IConfigValidator validator, IProxyConfigProvider proxyConfigProvider)
    {
        _validator = validator;
        _proxyConfigProvider = proxyConfigProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var config = _proxyConfigProvider.GetConfig();
        var errors = new List<Exception>();
        foreach (var configCluster in config.Clusters)
        {
            errors.AddRange(await _validator.ValidateClusterAsync(configCluster));
            if (configCluster.Destinations is null || configCluster.Destinations.Count == 0)
            {
                errors.Add(new OptionsValidationException("Custom",
                    typeof(ClusterConfig),
                    new[] { $"The cluster config: '{configCluster.ClusterId}.Destinations' can not be empty" }));
                continue;
            }

            foreach (var (key, destinationConfig) in configCluster.Destinations)
            {
                if (string.IsNullOrEmpty(destinationConfig.Address))
                {
                    errors.Add(new OptionsValidationException("Custom",
                        typeof(DestinationConfig),
                        new[]
                        {
                            $"The cluster config: '{configCluster.ClusterId}.Destinations.{key}.Address' is required"
                        }));
                }
            }
        }

        foreach (var configRoute in config.Routes)
        {
            errors.AddRange(await _validator.ValidateRouteAsync(configRoute));
        }

        if (errors.Count > 0)
        {
            throw new AggregateException(errors);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
