using LexBoxApi.Config;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace LexBoxApi.Services;

public class FwHeadlessHealthCheck(IHttpClientFactory clientFactory, IOptions<HealthChecksConfig> healthCheckOptions) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = new())
    {
        var http = clientFactory.CreateClient();
        var fwHeadlessResponse = await http.GetAsync("http://fw-headless/api/healthz");
        if (!fwHeadlessResponse.IsSuccessStatusCode)
        {
            if (healthCheckOptions.Value.RequireHealthyFwHeadlessContainer)
            {
                return HealthCheckResult.Unhealthy("fw-headless not repsonding to health check");
            }
            else
            {
                return HealthCheckResult.Degraded("fw-headless not repsonding to health check");
            }
        }
        var fwHeadlessVersion = fwHeadlessResponse.Headers.GetValues("lexbox-version").FirstOrDefault();
        if (healthCheckOptions.Value.RequireFwHeadlessContainerVersionMatch && string.IsNullOrEmpty(fwHeadlessVersion))
        {
            return HealthCheckResult.Degraded("fw-headless version check failed to return a value");
        }
        if (healthCheckOptions.Value.RequireFwHeadlessContainerVersionMatch && fwHeadlessVersion != AppVersionService.Version)
        {
            return HealthCheckResult.Degraded(
                $"api version: '{AppVersionService.Version}' fw-headless version: '{fwHeadlessVersion}' mismatch");
        }
        return HealthCheckResult.Healthy();
    }
}
