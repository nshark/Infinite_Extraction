using System.Diagnostics;

namespace IE.Model.Diagnostics;

/// <summary>
/// The project's logging system. Per-category x level, quiet by default in local
/// runs and verbose in shipped builds.
///
/// Never use a raw print/console API for diagnostics -- route through here so a
/// subsystem can be turned up or silenced independently. Warn and Error are always
/// compiled in; Info/Debug/Trace are gated on the IE_LOG_VERBOSE constant, which
/// Directory.Build.props defines in every configuration (including Release), so a
/// user's bug report still carries a real log.
/// </summary>
public static class Log
{
#if IE_LOG_VERBOSE
    /// <summary>
    /// True when verbose call sites survived compilation. Gate any helper that
    /// exists purely to pre-compute log data on THIS constant -- not on DEBUG,
    /// which would strip it from shipping builds and desync the two.
    /// </summary>
    public const bool VerboseCompiled = true;
#else
    public const bool VerboseCompiled = false;
#endif

    private static readonly LogLevel[] Levels =
        new LogLevel[Enum.GetValues<LogCategory>().Length];

    private static ILogSink? _sink;

    static Log() => Reset();

    /// <summary>Installs the destination for log lines. Pass null to discard output.</summary>
    public static void Install(ILogSink? sink) => _sink = sink;

    /// <summary>Silences every category and detaches the sink.</summary>
    public static void Reset()
    {
        _sink = null;
        for (int i = 0; i < Levels.Length; i++)
        {
            Levels[i] = LogLevel.Warn;
        }
    }

    public static void SetLevel(LogCategory category, LogLevel level) =>
        Levels[(int)category] = level;

    public static LogLevel GetLevel(LogCategory category) => Levels[(int)category];

    /// <summary>
    /// Applies a spec string such as "Controller:debug,View:info" or "all:trace".
    /// Unparseable entries are ignored rather than throwing -- a typo in an env var
    /// must never take down the game.
    /// </summary>
    public static void Configure(string? spec)
    {
        if (string.IsNullOrWhiteSpace(spec))
        {
            return;
        }

        foreach (string entry in spec.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            string[] parts = entry.Split(':', StringSplitOptions.TrimEntries);
            string categoryText = parts[0];
            LogLevel level = LogLevel.Debug;

            if (parts.Length > 1 && !Enum.TryParse(parts[1], ignoreCase: true, out level))
            {
                continue;
            }

            if (string.Equals(categoryText, "all", StringComparison.OrdinalIgnoreCase))
            {
                foreach (LogCategory everyCategory in Enum.GetValues<LogCategory>())
                {
                    SetLevel(everyCategory, level);
                }

                continue;
            }

            if (Enum.TryParse(categoryText, ignoreCase: true, out LogCategory category))
            {
                SetLevel(category, level);
            }
        }
    }

    /// <summary>True when a line at this level would be emitted for this category.</summary>
    public static bool IsEnabled(LogCategory category, LogLevel level) =>
        _sink is not null && level <= Levels[(int)category];

    // Error and Warn are unconditional: they must survive every build configuration.
    public static void Error(LogCategory category, string message) => Emit(LogLevel.Error, category, message);

    public static void Warn(LogCategory category, string message) => Emit(LogLevel.Warn, category, message);

    [Conditional("IE_LOG_VERBOSE")]
    public static void Info(LogCategory category, string message) => Emit(LogLevel.Info, category, message);

    [Conditional("IE_LOG_VERBOSE")]
    public static void Debug(LogCategory category, string message) => Emit(LogLevel.Debug, category, message);

    [Conditional("IE_LOG_VERBOSE")]
    public static void Trace(LogCategory category, string message) => Emit(LogLevel.Trace, category, message);

    private static void Emit(LogLevel level, LogCategory category, string message)
    {
        if (!IsEnabled(category, level))
        {
            return;
        }

        _sink!.Write(level, category, message);
    }
}
