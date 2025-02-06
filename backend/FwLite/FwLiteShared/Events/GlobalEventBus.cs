using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace FwLiteShared.Events;

public class GlobalEventBus(ILogger<GlobalEventBus> logger) : IDisposable
{
    private readonly Subject<IFwEvent> _globalEventSubject = new();

    public IObservable<IFwEvent> OnGlobalEvent => _globalEventSubject;
    public IObservable<AuthenticationChangedEvent> OnAuthenticationChanged => OnGlobalEvent.OfType<AuthenticationChangedEvent>();
    public void PublishEvent(IFwEvent @event)
    {
        if (!@event.IsGlobal) throw new ArgumentException($"Event {@event.GetType()} is not global");
        logger.LogInformation("Publishing global event {@event}", @event);
        _globalEventSubject.OnNext(@event);
    }

    public void Dispose()
    {
        _globalEventSubject.OnCompleted();
        _globalEventSubject.Dispose();
    }
}

public class JsEventListener : IDisposable
{
    private readonly ILogger<JsEventListener> _logger;
    //just a guess, this may need to be adjusted if we start losing events
    private const int MaxJsEventQueueSize = 10;
    private readonly Channel<IFwEvent> _jsEventChannel = Channel.CreateBounded<IFwEvent>(MaxJsEventQueueSize);
    public JsEventListener(ILogger<JsEventListener> logger, GlobalEventBus globalEventBus)
    {
        _logger = logger;
        globalEventBus.OnGlobalEvent.Subscribe(e =>
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
            return await _jsEventChannel.Reader.ReadAsync();
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
        _jsEventChannel.Writer.Complete();
    }
}
