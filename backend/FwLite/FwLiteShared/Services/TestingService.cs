using Microsoft.JSInterop;

namespace FwLiteShared.Services;

public class TestingService
{
    [JSInvokable]
    public void ThrowException()
    {
        throw new Exception("This is a test exception");
    }

    [JSInvokable]
    public async Task ThrowExceptionAsync()
    {
        await Task.Yield();
        throw new Exception("This is a test exception");
    }
}
