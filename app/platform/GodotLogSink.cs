using Godot;
using IE.Model.Diagnostics;

namespace IE.App.Platform;

/// <summary>
/// Routes the core logging system to Godot's output. The only place in the codebase
/// that is allowed to call an engine print API -- everything else goes through
/// <see cref="Log"/> so it can be enabled or silenced per subsystem.
///
/// Warnings and errors also go to the engine's error stream so they surface in the
/// editor's Debugger panel with a stack trace attached.
/// </summary>
public sealed class GodotLogSink : ILogSink
{
    public void Write(LogLevel level, LogCategory category, string message)
    {
        string line = $"[{category.ToString().ToUpperInvariant()}] {level.ToString().ToUpperInvariant()}: {message}";

        switch (level)
        {
            case LogLevel.Error:
                GD.PrintErr(line);
                GD.PushError(line);
                break;
            case LogLevel.Warn:
                GD.PrintErr(line);
                GD.PushWarning(line);
                break;
            default:
                GD.Print(line);
                break;
        }
    }
}
