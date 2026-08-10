namespace IE.Model.Diagnostics;

/// <summary>
/// Severity, ordered least-to-most verbose. A category emits a line when the line's
/// level is less than or equal to the category's configured level.
/// </summary>
public enum LogLevel
{
    Off = 0,
    Error = 1,
    Warn = 2,
    Info = 3,
    Debug = 4,
    Trace = 5,
}
