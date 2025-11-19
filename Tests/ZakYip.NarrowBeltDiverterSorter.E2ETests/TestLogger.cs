using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace ZakYip.NarrowBeltDiverterSorter.E2ETests;

/// <summary>
/// Xunit logger provider for test output (shared)
/// </summary>
public class TestLoggerProvider : ILoggerProvider
{
    private readonly ITestOutputHelper _output;

    public TestLoggerProvider(ITestOutputHelper output)
    {
        _output = output;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new TestLogger(_output, categoryName);
    }

    public void Dispose()
    {
    }
}

/// <summary>
/// Xunit logger implementation (shared)
/// </summary>
public class TestLogger : ILogger
{
    private readonly ITestOutputHelper _output;
    private readonly string _categoryName;

    public TestLogger(ITestOutputHelper output, string categoryName)
    {
        _output = output;
        _categoryName = categoryName;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        try
        {
            _output.WriteLine($"[{logLevel}] {_categoryName}: {formatter(state, exception)}");
            if (exception != null)
            {
                _output.WriteLine($"  Exception: {exception}");
            }
        }
        catch
        {
            // Ignore if output is not available
        }
    }
}

/// <summary>
/// Logger extension methods for tests
/// </summary>
public static class TestLoggerExtensions
{
    public static ILoggingBuilder AddXUnit(this ILoggingBuilder builder, ITestOutputHelper output)
    {
        builder.AddProvider(new TestLoggerProvider(output));
        return builder;
    }
}
