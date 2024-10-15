namespace LocalWebApp.Routes;

public static class FeedbackRoute
{
    public static void MapFeedbackRoutes(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/feedback/fw-lite", () =>
        {
            var version = "alpha";
            var os = Environment.OSVersion.Platform switch
            {
                PlatformID.Win32NT => "Windows",
                PlatformID.Unix => "Linux",
                PlatformID.MacOSX => "Mac",
                _ => "Other"
            };
            var url =
                $"https://docs.google.com/forms/d/e/1FAIpQLSdUdNufT3sdoBscY7vixguYnvtgpaw-hjX-z54BKi9KlYv4vw/viewform?usp=pp_url&entry.2102942583={version}&entry.1772086822={os}";
            return Results.Redirect(url, preserveMethod: true);
        });
    }
}
