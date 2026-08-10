using IE.Model.Diagnostics;
using IE.Tests.Fakes;
using Xunit;

namespace IE.Tests;

/// <summary>
/// Covers the logging system itself. Instrumentation is a shipped feature here, not
/// scaffolding, so its gating, spec parsing, and typo tolerance are held by tests
/// the same way gameplay rules are.
/// </summary>
public sealed class LogTests : IDisposable
{
    private readonly RecordingLogSink _sink = new();

    public LogTests()
    {
        Log.Reset();
        Log.Install(_sink);
    }

    public void Dispose() => Log.Reset();

    [Fact]
    public void VerboseCallSitesAreCompiledIntoEveryConfiguration()
    {
        Assert.True(Log.VerboseCompiled, "IE_LOG_VERBOSE was not defined; a shipped build would log nothing but warnings");
    }

    [Fact]
    public void EveryCategoryDefaultsToWarn()
    {
        foreach (LogCategory category in Enum.GetValues<LogCategory>())
        {
            Assert.Equal(LogLevel.Warn, Log.GetLevel(category));
        }
    }

    [Fact]
    public void WarnAndErrorSurviveTheDefaultLevel()
    {
        Log.Warn(LogCategory.Boot, "warned");
        Log.Error(LogCategory.Boot, "errored");
        Log.Info(LogCategory.Boot, "informed");

        Assert.Equal(2, _sink.Lines.Count);
        Assert.DoesNotContain("informed", _sink.MessagesFor(LogCategory.Boot));
    }

    [Fact]
    public void RaisingOneCategoryLeavesTheOthersQuiet()
    {
        Log.SetLevel(LogCategory.Undo, LogLevel.Trace);

        Log.Trace(LogCategory.Undo, "undo line");
        Log.Trace(LogCategory.Model, "model line");

        Assert.True(_sink.HasCategory(LogCategory.Undo));
        Assert.False(_sink.HasCategory(LogCategory.Model));
    }

    [Theory]
    [InlineData("Controller:debug", LogCategory.Controller, LogLevel.Debug)]
    [InlineData("view:TRACE", LogCategory.View, LogLevel.Trace)]
    [InlineData("Save:off", LogCategory.Save, LogLevel.Off)]
    public void ConfigureAppliesOneCategoryAtATime(string spec, LogCategory category, LogLevel expected)
    {
        Log.Configure(spec);

        Assert.Equal(expected, Log.GetLevel(category));
    }

    [Fact]
    public void ConfigureHandlesMultipleEntriesAndAll()
    {
        Log.Configure("all:trace,Save:error");

        Assert.Equal(LogLevel.Trace, Log.GetLevel(LogCategory.Boot));
        Assert.Equal(LogLevel.Trace, Log.GetLevel(LogCategory.View));
        Assert.Equal(LogLevel.Error, Log.GetLevel(LogCategory.Save));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Nonsense:debug")]
    [InlineData("Controller:shouting")]
    public void ConfigureIgnoresGarbageRatherThanThrowing(string? spec)
    {
        Log.Configure(spec);

        Assert.Equal(LogLevel.Warn, Log.GetLevel(LogCategory.Controller));
    }

    [Fact]
    public void IsEnabledIsFalseWithNoSinkInstalled()
    {
        Log.SetLevel(LogCategory.Boot, LogLevel.Trace);
        Log.Install(null);

        Assert.False(Log.IsEnabled(LogCategory.Boot, LogLevel.Error));
    }

    [Fact]
    public void ResetSilencesEverythingAndDetachesTheSink()
    {
        Log.Configure("all:trace");

        Log.Reset();
        Log.Trace(LogCategory.Boot, "after reset");

        Assert.Empty(_sink.Lines);
        Assert.Equal(LogLevel.Warn, Log.GetLevel(LogCategory.Boot));
    }
}
