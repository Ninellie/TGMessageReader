using Microsoft.Extensions.Logging;

namespace MessageReader;
public class FileLoggerProvider : ILoggerProvider
{
    private readonly string? _filePath;

    public FileLoggerProvider(string fileName)
    {
        _filePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(_filePath);
    }

    public void Dispose()
    {
    }
}

public class FileLogger : ILogger
{
    private static readonly object Lock = new();
    private readonly string _filePath;

    public FileLogger(string filePath)
    {
        _filePath = filePath;
    }

    public IDisposable? BeginScope<TState>(TState state)
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        //return logLevel == LogLevel.Trace;
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        if (formatter == null) return;
        lock (Lock)
        {
            if (_filePath != null) File.AppendAllText(_filePath, DateTime.Now.ToLocalTime() + " - "+ formatter(state, exception) + Environment.NewLine);
        }
    }
}