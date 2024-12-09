using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using LexBoxApi.Otel;
using LexBoxApi.Services.FwLiteReleases;
using LexCore.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Hybrid;

namespace LexBoxApi.Controllers;

[ApiController]
[Route("/api/fwlite-release")]
[ApiExplorerSettings(GroupName = LexBoxKernel.OpenApiPublicDocumentName)]
public class FwLiteReleaseController(FwLiteReleaseService releaseService) : ControllerBase
{

    [HttpGet("download-latest")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DownloadLatest()
    {
        using var activity = LexBoxActivitySource.Get().StartActivity();
        var latestRelease = await releaseService.GetLatestRelease();
        if (latestRelease is null)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Latest release not found");
            return NotFound();
        }
        activity?.AddTag(FwLiteReleaseService.FwLiteReleaseVersionTag, latestRelease.Version);
        return Redirect(latestRelease.Url);
    }

    [HttpGet("latest")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesDefaultResponseType]
    public async ValueTask<ActionResult<FwLiteRelease>> LatestRelease(string? appVersion = null)
    {
        using var activity = LexBoxActivitySource.Get().StartActivity();
        activity?.AddTag(FwLiteReleaseService.FwLiteClientVersionTag, appVersion ?? "unknown");
        var latestRelease = await releaseService.GetLatestRelease();
        activity?.AddTag(FwLiteReleaseService.FwLiteReleaseVersionTag, latestRelease?.Version);
        if (latestRelease is null) return NotFound();
        return latestRelease;
    }

    [HttpGet("should-update")]
    public async Task<ActionResult<ShouldUpdateResponse>> ShouldUpdate([FromQuery] string appVersion)
    {
        using var activity = LexBoxActivitySource.Get().StartActivity();
        activity?.AddTag(FwLiteReleaseService.FwLiteClientVersionTag, appVersion);
        var response = await releaseService.ShouldUpdate(appVersion);
        activity?.AddTag(FwLiteReleaseService.FwLiteReleaseVersionTag, response.Release?.Version);
        return response;
    }

    [HttpPost("new-release")]
    [AllowAnonymous]
    public async Task<OkResult> NewRelease()
    {
        await releaseService.InvalidateReleaseCache();
        return Ok();
    }
}
