using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace FwLiteShared.Events;

public class JsEventListener : IDisposable
{
    private readonly ILogger<JsEventListener> _logger;
    //just a guess, this may need to be adjusted if we start losing events
    private const int MaxJsEventQueueSize = 10;
    private readonly Channel<IFwEvent> _jsEventChannel = Channel.CreateBounded<IFwEvent>(MaxJsEventQueueSize);
    private readonly IDisposable _globalBusSubscription;

    private static readonly Type[] ValidEventTypes = typeof(IFwEvent).GetCustomAttributes<JsonDerivedTypeAttribute>()
        .Select(a => a.DerivedType).ToArray();

    public JsEventListener(ILogger<JsEventListener> logger, GlobalEventBus globalEventBus)
    {
        _logger = logger;
        _globalBusSubscription = globalEventBus.OnGlobalEvent.Subscribe(e =>
        {
            if (_jsEventChannel.Writer.TryWrite(e))
            {
                return;
            }
            _logger.LogError("Failed to write js event, channel is full");
        });
    }

    /// <summary>
    /// returns null when there are no more events to read
    /// </summary>
    [JSInvokable]
    public async ValueTask<IFwEvent?> NextEventAsync()
    {
        try
        {
            var e = await _jsEventChannel.Reader.ReadAsync();
            if (_logger.IsEnabled(LogLevel.Trace))
                _logger.LogTrace("Received js event {Event}, json: {Json}", e.Type, JsonSerializer.Serialize(e));
            if (!ValidEventTypes.Contains(e.GetType()))
            {
                _logger.LogError("Received invalid event type {Type}", e.GetType());
            }
            return e;
        }
        catch (ChannelClosedException)
        {
            return null;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error reading js event");
            throw;
        }
    }

    public void Dispose()
    {
        _globalBusSubscription.Dispose();
        _jsEventChannel.Writer.Complete();
    }
}
