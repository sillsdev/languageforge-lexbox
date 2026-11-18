using FwLiteShared.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FwLiteShared.Tests.Events;

public class JsEventListenerTests
{
    private readonly GlobalEventBus _bus = new(NullLogger<GlobalEventBus>.Instance);
    private readonly CapturingLogger _logger = new();

    [Fact]
    public async Task NextEventsAsync_ReturnsAllQueuedEventsInPublishOrder()
    {
        using var listener = new JsEventListener(_logger, _bus);
        _bus.PublishEvent(new UserNotificationEvent("first"));
        _bus.PublishEvent(new UserNotificationEvent("second"));
        _bus.PublishEvent(new UserNotificationEvent("third"));

        var events = await listener.NextEventsAsync();

        Messages(events).Should().Equal("first", "second", "third");
    }

    [Fact]
    public async Task NextEventsAsync_WaitsForTheNextEventWhenNoneAreQueued()
    {
        using var listener = new JsEventListener(_logger, _bus);

        var pending = listener.NextEventsAsync().AsTask();
        pending.IsCompleted.Should().BeFalse();

        _bus.PublishEvent(new UserNotificationEvent("late"));

        Messages(await pending).Should().Equal("late");
    }

    [Fact]
    public async Task FullQueue_KeepsTheFirst100Events_AndLogsOneErrorAndOneWarning()
    {
        using var listener = new JsEventListener(_logger, _bus);
        for (var i = 1; i <= 105; i++)
            _bus.PublishEvent(new UserNotificationEvent($"event {i}"));

        var events = await listener.NextEventsAsync();

        var expected = Enumerable.Range(1, 100).Select(i => $"event {i}");
        Messages(events).Should().Equal(expected);
        _logger.Entries.Should().Equal(
            (LogLevel.Error, "JS event queue is full (100), dropping events until JS catches up"),
            (LogLevel.Warning, "JS event queue drained after dropping 5 events"));
    }

    [Fact]
    public async Task FullQueue_LogsAgain_AfterItDrainsAndFillsUpAgain()
    {
        using var listener = new JsEventListener(_logger, _bus);
        for (var i = 0; i < 101; i++) _bus.PublishEvent(new UserNotificationEvent("first spell"));
        await listener.NextEventsAsync();
        for (var i = 0; i < 101; i++) _bus.PublishEvent(new UserNotificationEvent("second spell"));
        await listener.NextEventsAsync();

        _logger.Entries.Should().Equal(
            (LogLevel.Error, "JS event queue is full (100), dropping events until JS catches up"),
            (LogLevel.Warning, "JS event queue drained after dropping 1 events"),
            (LogLevel.Error, "JS event queue is full (100), dropping events until JS catches up"),
            (LogLevel.Warning, "JS event queue drained after dropping 1 events"));
    }

    [Fact]
    public async Task NextEventsAsync_ReturnsEmpty_OnceDisposed()
    {
        var listener = new JsEventListener(_logger, _bus);
        var pending = listener.NextEventsAsync().AsTask();

        listener.Dispose();

        (await pending).Should().BeEmpty();
    }

    private static string[] Messages(IFwEvent[] events) =>
        events.Cast<UserNotificationEvent>().Select(e => e.Message).ToArray();

    private class CapturingLogger : ILogger<JsEventListener>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            lock (Entries) Entries.Add((logLevel, formatter(state, exception)));
        }

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Debug;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    }
}
