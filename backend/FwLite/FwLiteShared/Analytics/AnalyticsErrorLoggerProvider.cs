using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FwLiteShared.Analytics;

/// <summary>
/// Reports every <see cref="LogLevel.Error"/> and <see cref="LogLevel.Critical"/> log entry to Mixpanel as an
/// <see cref="ErrorReporting.ErrorEvent"/>. Hooking logging covers unhandled exceptions in both hosts, framework
/// errors (ASP.NET, Blazor) and viewer errors (logged via <c>JsInvokableLogger</c>) without extra try/catch blocks.
/// Respects the analytics opt-out like every other event.
/// </summary>
public sealed class AnalyticsErrorLoggerProvider(IServiceProvider services, TimeProvider? timeProvider = null)
    : ILoggerProvider
{
    /// <summary>Guards Mixpanel quota against an error loop; not meant to filter noise.</summary>
    internal const int MaxReportsPerMinute = 60;

    // The analytics pipeline's own failures are logged as warnings, but never feed them back in regardless.
    private static readonly string[] ExcludedCategoryPrefixes =
    [
        "FwLiteShared.Analytics",
        "LexCore.Analytics",
        "System.Net.Http.HttpClient." + MixpanelAnalytics.HttpClientName,
    ];

    [ThreadStatic] private static bool _reporting;

    private readonly TimeProvider _clock = timeProvider ?? TimeProvider.System;
    private readonly Lock _rateLock = new();
    private DateTimeOffset _windowStart = DateTimeOffset.MinValue;
    private int _windowCount;
    private IAnalyticsService? _analytics;

    public ILogger CreateLogger(string categoryName) => new ErrorLogger(this, categoryName);

    public void Dispose()
    {
    }

    private static bool IsExcluded(string category) =>
        ExcludedCategoryPrefixes.Any(prefix => category.StartsWith(prefix, StringComparison.Ordinal));

    internal void Report(string category, LogLevel level, string? messageTemplate, Exception? exception)
    {
        if (_reporting)
            return;
        _reporting = true;
        try
        {
            // Resolved lazily: AnalyticsService itself depends on ILogger, so it can't be injected here.
            var analytics = _analytics ??= services.GetService<IAnalyticsService>();
            if (analytics is null || !analytics.GetAnalyticsEnabled() || !TryTakeRateSlot())
                return;
            analytics.Track(ErrorReporting.ErrorEvent,
                ErrorReporting.BuildProperties(category, level, messageTemplate, exception));
        }
        catch
        {
            // Logging must never throw, and there is nowhere left to report a failure to.
        }
        finally
        {
            _reporting = false;
        }
    }

    private bool TryTakeRateSlot()
    {
        lock (_rateLock)
        {
            var now = _clock.GetUtcNow();
            if (now - _windowStart >= TimeSpan.FromMinutes(1))
            {
                _windowStart = now;
                _windowCount = 0;
            }
            if (_windowCount >= MaxReportsPerMinute)
                return false;
            _windowCount++;
            return true;
        }
    }

    private sealed class ErrorLogger(AnalyticsErrorLoggerProvider provider, string category) : ILogger
    {
        private readonly bool _excluded = IsExcluded(category);

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => !_excluded && logLevel >= LogLevel.Error && logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;
            provider.Report(category, logLevel, MessageTemplate(state, exception, formatter), exception);
        }

        /// <summary>
        /// Prefer the message template ("Failed to open {Project}") over the formatted message, which embeds
        /// argument values that may be user data.
        /// </summary>
        private static string? MessageTemplate<TState>(TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (state is IReadOnlyList<KeyValuePair<string, object?>> values)
            {
                foreach (var (key, value) in values)
                {
                    if (key == "{OriginalFormat}")
                        return value as string;
                }
            }
            return formatter(state, exception);
        }
    }
}
