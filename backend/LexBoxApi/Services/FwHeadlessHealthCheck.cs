using LexCore.Config;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace LexBoxApi.Services;

public class FwHeadlessHealthCheckCheckHealthAsync(IHttpClientFactory clientFactory, IOptions<HgConfig> hgOptions) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = new())
    {
        var http = clientFactory.CreateClient();
        var fwHeadlessResponse = await http.GetAsync("http://fw-headless/api/healthz");
        if (!fwHeadlessResponse.IsSuccessStatusCode)
        {
            return HealthCheckResult.Degraded("fw-headless not repsonding to health check");
        }
        var fwHeadlessVersion = fwHeadlessResponse.Headers.GetValues("lexbox-version").FirstOrDefault();
        if (string.IsNullOrEmpty(fwHeadlessVersion))
        {
            return HealthCheckResult.Degraded("fw-headless version check failed to return a value");
        }
        // TODO: Decide if we would ever want fwHeadlessOptions.RequireContainerVersionMatch to be a different value than hgOptions
        // If so, then create FwHeadlessOptions, even if it would only have one single value in it
        if (hgOptions.Value.RequireContainerVersionMatch && fwHeadlessVersion != AppVersionService.Version)
        {
            return HealthCheckResult.Degraded(
                $"api version: '{AppVersionService.Version}' fw-headless version: '{fwHeadlessVersion}' mismatch");
        }
        return HealthCheckResult.Healthy();
    }
}
