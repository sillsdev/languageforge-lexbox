using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

namespace LexBoxApi.Models;

/// <summary>
/// by default if you set IAsyncEnumerable<T> as a parameter type, it will be created using JsonSerializer.DeserializeAsync
/// this will buffer the data into a list, rather than streaming it. This class exists to allow streaming
/// </summary>
[UseStreamingBinder]
public class StreamJsonAsyncEnumerable<T>(IAsyncEnumerable<T> stream): IAsyncEnumerable<T>
{
    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = new CancellationToken())
    {
        return stream.GetAsyncEnumerator(cancellationToken);
    }
}

internal class UseStreamingBinderAttribute() : ModelBinderAttribute(typeof(StreamJsonAsyncEnumerableConverter))
{
    public override BindingSource? BindingSource => BindingSource.Body;
}
public class StreamJsonAsyncEnumerableConverter(ILogger<StreamJsonAsyncEnumerableConverter> logger): IModelBinder
{
    private static readonly MethodInfo BindMethod = new Action<ModelBindingContext>(Bind<object>).Method.GetGenericMethodDefinition();
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (!bindingContext.ModelType.IsGenericType)
        {
            throw new InvalidOperationException("Model type must be generic");
        }
        try
        {
            var genericType = bindingContext.ModelType.GetGenericArguments()[0];
            var bindMethodGeneric = BindMethod.MakeGenericMethod(genericType);
            bindMethodGeneric.Invoke(null, [bindingContext]);
        }
        catch (Exception e)
        {
            logger.LogError(e, "problem deserializing json");
            bindingContext.ModelState.TryAddModelException("this", e);
        }
        return Task.CompletedTask;
    }

    private static void Bind<T>(ModelBindingContext bindingContext) where T : class
    {
        var options = bindingContext.HttpContext.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value
            .JsonSerializerOptions;
        var enumerable = JsonSerializer.DeserializeAsyncEnumerable<T>(bindingContext.HttpContext.Request.BodyReader.AsStream(), options, bindingContext.HttpContext.RequestAborted);
        bindingContext.Result = ModelBindingResult.Success(new StreamJsonAsyncEnumerable<T>(enumerable.OfType<T>()));
    }
}
