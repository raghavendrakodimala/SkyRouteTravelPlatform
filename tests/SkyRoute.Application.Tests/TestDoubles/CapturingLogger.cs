using Microsoft.Extensions.Logging;

namespace SkyRoute.Application.Tests.TestDoubles;

/// <summary>
/// Minimal ILogger&lt;T&gt; test double capturing log level, message, and exception, so tests
/// can assert on NFR-OBS-001/NFR-AVAIL-003 ("a log entry is recorded containing the failing
/// provider's name and exception message/type" — test-strategy.md Section 6) without pulling
/// in a mocking library.
/// </summary>
public sealed class CapturingLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Entries.Add((logLevel, formatter(state, exception), exception));
    }
}
