using LexCore.Config;
using LexCore.ServiceInterfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace LexBoxApi.Services;

public class HgWebHealthCheck(IHgService hgService, IOptions<HgConfig> hgOptions) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = new())
    {
        var version = await hgService.HgCommandHealth();
        if (string.IsNullOrEmpty(version))
        {
            return HealthCheckResult.Unhealthy();
        }
        if (hgOptions.Value.RequireContainerVersionMatch && version != AppVersionService.Version)
        {
            return HealthCheckResult.Degraded(
                $"api version: '{AppVersionService.Version}' hg version: '{version}' mismatch");
        }
        return HealthCheckResult.Healthy();
    }
}
