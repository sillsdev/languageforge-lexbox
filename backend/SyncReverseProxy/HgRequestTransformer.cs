using System.Text.RegularExpressions;
using Yarp.ReverseProxy.Forwarder;

namespace LexSyncReverseProxy;

public partial class HgRequestTransformer : HttpTransformer
{
    [GeneratedRegex(@"^/hg/[a-z-0-9]/")]
    private static partial Regex HasFirstLetterPrefix();

    public override async ValueTask TransformRequestAsync(HttpContext httpContext,
        HttpRequestMessage proxyRequest,
        string destinationPrefix,
        CancellationToken cancellationToken)
    {
        await base.TransformRequestAsync(httpContext, proxyRequest, destinationPrefix, cancellationToken);
        var path = httpContext.Request.Path.ToString();
        if (path.StartsWith("/hg")) path = path["/hg".Length..];
        var builder = new UriBuilder(RequestUtilities.MakeDestinationAddress(destinationPrefix,
            path,
            httpContext.Request.QueryString));
        var projectCode = httpContext.Request.GetProjectCode();
        if (projectCode is not null && projectCode.Length > 0)
        {
            //rewrite project code to be in the format of /{first letter}/{project code}
            if (builder.Path.StartsWith("/api/v03"))
            {
                builder.Query = builder.Query.Replace(projectCode, $"{projectCode[0]}/{projectCode}");
            }
            else if (HasFirstLetterPrefix().IsMatch(builder.Path))
            {
                //don't modify requests that already have the first letter in the path
            }
            else
            {
                builder.Path = builder.Path.Replace(projectCode, $"{projectCode[0]}/{projectCode}");
            }
        }

        proxyRequest.RequestUri = builder.Uri;
    }
}
