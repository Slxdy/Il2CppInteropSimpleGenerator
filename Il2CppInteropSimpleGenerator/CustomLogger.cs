using Microsoft.Extensions.Logging;

namespace Il2CppInteropSimpleGenerator;

internal class CustomLogger : ILogger
{
    private LogLevel lowestLevel;
    private string loggerName;

    public CustomLogger(string loggerName, LogLevel lowestLogLevel)
    {
        this.loggerName = loggerName;
        this.lowestLevel = lowestLogLevel;
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        return new EmptyScope();
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= lowestLevel;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Program.Log($"[{loggerName}] [{logLevel}] {state} {exception?.ToString() ?? ""}");
    }

    private class EmptyScope : IDisposable
    {
        public void Dispose() { }
    }
}
