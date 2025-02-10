using System.Text.RegularExpressions;
using Microsoft.Net.Http.Headers;
using Yarp.ReverseProxy.Forwarder;
using MediaTypeHeaderValue = System.Net.Http.Headers.MediaTypeHeaderValue;

namespace LexSyncReverseProxy;

public partial class HgRequestTransformer : HttpTransformer
{
    [GeneratedRegex(@"^/hg/[a-z0-9]/")]
    private static partial Regex HasFirstLetterPrefix();

    public override async ValueTask TransformRequestAsync(HttpContext httpContext,
        HttpRequestMessage proxyRequest,
        string destinationPrefix,
        CancellationToken cancellationToken)
    {
        await base.TransformRequestAsync(httpContext, proxyRequest, destinationPrefix, cancellationToken);

        // Remove the cookie header from the request
        proxyRequest.Headers.Remove(HeaderNames.Cookie);
        proxyRequest.Headers.Remove(HeaderNames.Authorization);

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

    public override async ValueTask<bool> TransformResponseAsync(HttpContext httpContext,
        HttpResponseMessage? proxyResponse,
        CancellationToken cancellationToken)
    {
        if (proxyResponse?.RequestMessage?.RequestUri?.Query.Contains("cmd=capabilities") == true)
        {
            var originalRequestContent = proxyResponse.Content;
            var responseString = await originalRequestContent.ReadAsStringAsync(cancellationToken);
            responseString = responseString.Replace("unbundle=HG10GZ,HG10BZ,HG10UN", "unbundle=HG10GZ,HG10BZ");
            proxyResponse.Content = new StringContent(responseString, new MediaTypeHeaderValue("application/mercurial-0.1"))
            {
            };
            // proxyResponse.Content.Headers.Clear();
            // foreach (var httpContentHeader in originalRequestContent.Headers)
            // {
                // proxyResponse.Content.Headers.Add(httpContentHeader.Key, httpContentHeader.Value);
            // }
        }
        return await base.TransformResponseAsync(httpContext, proxyResponse, cancellationToken);
    }
}
