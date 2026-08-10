using IE.Model.Diagnostics;

namespace IE.Tests.Fakes;

/// <summary>Captures log output so a test can assert a code path actually ran.</summary>
public sealed class RecordingLogSink : ILogSink
{
    public List<(LogLevel Level, LogCategory Category, string Message)> Lines { get; } = new();

    public void Write(LogLevel level, LogCategory category, string message) =>
        Lines.Add((level, category, message));

    public bool HasCategory(LogCategory category) => Lines.Exists(line => line.Category == category);

    public IEnumerable<string> MessagesFor(LogCategory category) =>
        Lines.Where(line => line.Category == category).Select(line => line.Message);
}
