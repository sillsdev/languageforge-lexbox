using Microsoft.JSInterop;

namespace FwLiteShared;

public static class JsInteropInvokeHelper
{
    public static async ValueTask DurableInvokeVoidAsync(this IJSRuntime jsRuntime, string identifier, params object?[] args)
    {
        await jsRuntime.InvokeVoidAsync("invokeOnWindow", identifier, args);
    }
}
