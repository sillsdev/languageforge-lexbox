using LexBoxApi.Config;
using Microsoft.Extensions.Options;

namespace LexBoxApi.Services;

public class LexboxLinkGenerator : LinkGenerator
{
    private readonly LinkGenerator _linkGenerator;
    private readonly string _host;
    private readonly string _scheme;


    public LexboxLinkGenerator(LinkGenerator linkGenerator, IOptions<EmailConfig> emailConfig)
    {
        _linkGenerator = linkGenerator;
        var uri = new UriBuilder(emailConfig.Value.BaseUrl).Uri;
        _host = uri.Authority;
        _scheme = uri.Scheme;
    }


    public override string? GetPathByAddress<TAddress>(HttpContext httpContext,
        TAddress address,
        RouteValueDictionary values,
        RouteValueDictionary? ambientValues = null,
        PathString? pathBase = null,
        FragmentString fragment = new FragmentString(),
        LinkOptions? options = null)
    {
        return _linkGenerator.GetPathByAddress(httpContext,
            address,
            values,
            ambientValues,
            pathBase,
            fragment,
            options);
    }

    public override string? GetPathByAddress<TAddress>(TAddress address,
        RouteValueDictionary values,
        PathString pathBase = new PathString(),
        FragmentString fragment = new FragmentString(),
        LinkOptions? options = null)
    {
        return _linkGenerator.GetPathByAddress(address, values, pathBase, fragment, options);
    }

    public override string? GetUriByAddress<TAddress>(HttpContext httpContext,
        TAddress address,
        RouteValueDictionary values,
        RouteValueDictionary? ambientValues = null,
        string? scheme = null,
        HostString? host = null,
        PathString? pathBase = null,
        FragmentString fragment = new FragmentString(),
        LinkOptions? options = null)
    {
        return _linkGenerator.GetUriByAddress(httpContext,
            address,
            values,
            ambientValues,
            scheme,
            host ?? new HostString(_host),
            pathBase,
            fragment,
            options);
    }

    public override string? GetUriByAddress<TAddress>(TAddress address,
        RouteValueDictionary values,
        string? scheme,
        HostString host,
        PathString pathBase = new PathString(),
        FragmentString fragment = new FragmentString(),
        LinkOptions? options = null)
    {
        return _linkGenerator.GetUriByAddress(address, values, scheme ?? _scheme, host, pathBase, fragment, options);
    }
}
