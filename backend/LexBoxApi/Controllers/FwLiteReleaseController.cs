using System.Diagnostics;
using System.Text;
using LexBoxApi.Otel;
using LexBoxApi.Services.FwLiteReleases;
using LexCore.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LexBoxApi.Controllers;

[ApiController]
[Route("/api/fwlite-release")]
[ApiExplorerSettings(GroupName = LexboxOpenApi.OpenApiPublicDocumentName)]
public class FwLiteReleaseController(FwLiteReleaseService releaseService) : ControllerBase
{
    //The App Installer track requires the manifest to be served from a URL whose path ends in
    //.appinstaller: Add-AppxPackage -AppInstallerFile and PackageManager.AddPackageByAppInstallerFileAsync
    //validate the extension on the parsed path (a ?...=.appinstaller query does not satisfy it). This is
    //the canonical manifest URL baked into every installed package as its update source, so keep it stable.
    [HttpGet("FieldWorksLite.appinstaller")]
    [AllowAnonymous]
    public async Task<ActionResult> AppInstaller()
    {
        using var activity = LexBoxActivitySource.Get().StartActivity();
        activity?.AddTag(FwLiteReleaseService.FwLiteEditionTag, FwLiteEdition.WindowsAppInstaller.ToString());
        var appInstallerContent = await releaseService.GenerateAppInstaller();
        return File(Encoding.UTF8.GetBytes(appInstallerContent), "application/appinstaller", "FieldWorksLite.appinstaller");
    }

    [HttpGet("download-latest")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DownloadLatest([FromQuery] FwLiteEdition edition = FwLiteEdition.Windows)
    {
        using var activity = LexBoxActivitySource.Get().StartActivity();
        activity?.AddTag(FwLiteReleaseService.FwLiteEditionTag, edition.ToString());
        if (edition == FwLiteEdition.WindowsAppInstaller)
        {
            //Redirect to the canonical .appinstaller path; the App Installer APIs reject a URL that
            //doesn't end in .appinstaller, so that path (not this query URL) must be the update source.
            return RedirectToAction(nameof(AppInstaller));
        }
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
