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

    private readonly GlobalEventBus _globalEventBus;

    // Holds events while JS is busy or not yet listening. A circuit whose tab is gone is kept for a few minutes
    // and fills this up, so being full is expected there; we log once per full spell rather than per event.
    internal const int MaxJsEventQueueSize = 100;
    private readonly Channel<IFwEvent> _jsEventChannel = Channel.CreateBounded<IFwEvent>(
        new BoundedChannelOptions(MaxJsEventQueueSize) { SingleReader = true });
    private readonly IDisposable _globalBusSubscription;
    private int _droppedEventCount;

    private static readonly Type[] ValidEventTypes = typeof(IFwEvent).GetCustomAttributes<JsonDerivedTypeAttribute>()
        .Select(a => a.DerivedType).ToArray();

    public JsEventListener(ILogger<JsEventListener> logger, GlobalEventBus globalEventBus)
    {
        _logger = logger;
        _globalEventBus = globalEventBus;
        _globalBusSubscription = globalEventBus.OnGlobalEvent.Subscribe(e =>
        {
            if (_jsEventChannel.Writer.TryWrite(e))
            {
                return;
            }
            if (Interlocked.Increment(ref _droppedEventCount) == 1)
                _logger.LogError("JS event queue is full ({Size}), dropping events until JS catches up", MaxJsEventQueueSize);
        });
    }

    /// <summary>
    /// Waits for at least one event, then returns every event that is queued, oldest first.
    /// Returns an empty array when the listener is closed and there are no more events to read.
    /// </summary>
    [JSInvokable]
    public async ValueTask<IFwEvent[]> NextEventsAsync()
    {
        try
        {
            if (!await _jsEventChannel.Reader.WaitToReadAsync()) return [];
            var events = new List<IFwEvent>();
            while (_jsEventChannel.Reader.TryRead(out var e))
            {
                if (_logger.IsEnabled(LogLevel.Trace))
                    _logger.LogTrace("Received js event {Event}, json: {Json}", e.Type, JsonSerializer.Serialize(e));
                if (!ValidEventTypes.Contains(e.GetType()))
                {
                    _logger.LogError("Received invalid event type {Type}", e.GetType());
                }
                events.Add(e);
            }

            var dropped = Interlocked.Exchange(ref _droppedEventCount, 0);
            if (dropped > 0)
                _logger.LogWarning("JS event queue drained after dropping {Count} events", dropped);
            return [.. events];
        }
        catch (ChannelClosedException)
        {
            return [];
        }
        catch (OperationCanceledException)
        {
            return [];
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error reading js event");
            throw;
        }
    }

    [JSInvokable]
    public ValueTask<IFwEvent?> LastEvent(FwEventType type)
    {
        return ValueTask.FromResult(_globalEventBus.GetLastEvent(type));
    }

    public void Dispose()
    {
        _globalBusSubscription.Dispose();
        _jsEventChannel.Writer.Complete();
    }
}
