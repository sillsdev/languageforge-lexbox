using LexCore.ServiceInterfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LexBoxApi.Services;

public class HgWebHealthCheck(IHgService hgService) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = new())
    {
        var version = await hgService.HgCommandHealth();
        if (string.IsNullOrEmpty(version))
        {
            return HealthCheckResult.Unhealthy();
        }
        if (version != AppVersionService.Version)
        {
            return HealthCheckResult.Degraded(
                $"api version: '{AppVersionService.Version}' hg version: '{version}' mismatch");
        }
        return HealthCheckResult.Healthy();
    }
}
