namespace IE.Model.Diagnostics;

/// <summary>
/// Where formatted log lines go. The engine supplies one implementation, tests
/// supply a recording one. Keeps the core free of any platform print API.
/// </summary>
public interface ILogSink
{
    void Write(LogLevel level, LogCategory category, string message);
}
