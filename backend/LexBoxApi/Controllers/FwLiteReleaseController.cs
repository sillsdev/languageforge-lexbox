﻿using System.Diagnostics;
using LexBoxApi.Config;
using LexBoxApi.Otel;
using LexBoxApi.Services.FwLiteReleases;
using LexCore.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LexBoxApi.Controllers;

[ApiController]
[Route("/api/fwlite-release")]
[ApiExplorerSettings(GroupName = LexBoxKernel.OpenApiPublicDocumentName)]
public class FwLiteReleaseController(FwLiteReleaseService releaseService) : ControllerBase
{

    [HttpGet("download-latest")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DownloadLatest([FromQuery] FwLiteEdition edition = FwLiteEdition.Windows)
    {
        using var activity = LexBoxActivitySource.Get().StartActivity();
        activity?.AddTag(FwLiteReleaseService.FwLiteEditionTag, edition.ToString());
        var latestRelease = await releaseService.GetLatestRelease(edition);
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
    public async ValueTask<ActionResult<FwLiteRelease>> LatestRelease([FromQuery] FwLiteEdition edition =
        FwLiteEdition.Windows, string? appVersion = null)
    {
        using var activity = LexBoxActivitySource.Get().StartActivity();
        activity?.AddTag(FwLiteReleaseService.FwLiteClientVersionTag, appVersion ?? "unknown");
        activity?.AddTag(FwLiteReleaseService.FwLiteEditionTag, edition.ToString());
        var latestRelease = await releaseService.GetLatestRelease(edition);
        activity?.AddTag(FwLiteReleaseService.FwLiteReleaseVersionTag, latestRelease?.Version);
        if (latestRelease is null) return NotFound();
        return latestRelease;
    }

    [HttpGet("should-update")]
    [AllowAnonymous]
    public async Task<ActionResult<ShouldUpdateResponse>> ShouldUpdate([FromQuery] string appVersion, [FromQuery] FwLiteEdition edition = FwLiteEdition.Windows)
    {
        using var activity = LexBoxActivitySource.Get().StartActivity();
        activity?.AddTag(FwLiteReleaseService.FwLiteClientVersionTag, appVersion);
        activity?.AddTag(FwLiteReleaseService.FwLiteEditionTag, edition.ToString());
        var response = await releaseService.ShouldUpdate(edition, appVersion);
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
