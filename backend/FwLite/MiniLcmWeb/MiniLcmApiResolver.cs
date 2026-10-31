using Microsoft.AspNetCore.Http;
using MiniLcm;

namespace MiniLcmWeb;

/// <summary>
/// Opens the <see cref="IMiniLcmApi"/> a MiniLcm route acts on, from the request's route values.
/// Each host decides what identifies a project (FwLiteWeb: data format and code, FwHeadless: project id).
/// </summary>
public delegate ValueTask<MiniLcmApiResolution> MiniLcmApiResolver(HttpContext context);

/// <summary>
/// Either the api for the request, or the response to send instead (e.g. a 404 problem for an unknown project).
/// </summary>
public readonly record struct MiniLcmApiResolution
{
    public IMiniLcmApi? Api { get; private init; }
    public IResult? Error { get; private init; }

    public static MiniLcmApiResolution Resolved(IMiniLcmApi api) => new() { Api = api };
    public static MiniLcmApiResolution Failed(IResult error) => new() { Error = error };
}
