using Microsoft.AspNetCore.Mvc;

namespace LexBoxApi.Controllers;

[ApiController]
[Route("/api/feedback")]
public class FeedbackController() : ControllerBase
{
    [HttpGet("fw-lite")]
    public IResult RedirectToFieldWorksLiteFeedbackForm()
    {
        var version = "alpha";
        var os = "Web";
        var url = $"https://docs.google.com/forms/d/e/1FAIpQLSdUdNufT3sdoBscY7vixguYnvtgpaw-hjX-z54BKi9KlYv4vw/viewform?usp=pp_url&entry.2102942583={version}&entry.1772086822={os}";
        return Results.Redirect(url, preserveMethod: true);
    }
}
