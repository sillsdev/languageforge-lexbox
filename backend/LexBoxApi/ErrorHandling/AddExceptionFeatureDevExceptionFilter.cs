using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Features;

namespace LexBoxApi.ErrorHandling;

public class AddExceptionFeatureDevExceptionFilter : IDeveloperPageExceptionFilter
{
    public async Task HandleExceptionAsync(ErrorContext errorContext, Func<ErrorContext, Task> next)
    {
        var httpContext = errorContext.HttpContext;
        var features = httpContext.Features;
        if (features.Get<IExceptionHandlerFeature>() is null)
        {
            features.Set<IExceptionHandlerFeature>(new ExceptionHandlerFeature
            {
                Error = errorContext.Exception,
                Path = httpContext.Request.Path,
                Endpoint = httpContext.GetEndpoint(),
                RouteValues = features.Get<IRouteValuesFeature>()?.RouteValues
            });
        }
        await next(errorContext);
    }
}
