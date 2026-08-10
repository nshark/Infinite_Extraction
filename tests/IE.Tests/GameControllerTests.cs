using IE.Controller;
using IE.Model.Diagnostics;
using IE.Tests.Fakes;
using Xunit;

namespace IE.Tests;

/// <summary>
/// Covers the controller shell: boot paints exactly once through the single refresh
/// funnel, and says so in the log. Handler and undo coverage returns alongside the
/// first mutating handler.
/// </summary>
public sealed class GameControllerTests : IDisposable
{
    private readonly RecordingLogSink _sink = new();
    private readonly FakeMainView _view = new();
    private readonly SynchronousPacer _pacer = new();
    private readonly SessionState _session = new();

    public GameControllerTests()
    {
        Log.Reset();
        Log.Install(_sink);
        Log.SetLevel(LogCategory.Boot, LogLevel.Trace);
        Log.SetLevel(LogCategory.Controller, LogLevel.Trace);
        Log.SetLevel(LogCategory.View, LogLevel.Trace);
    }

    public void Dispose() => Log.Reset();

    private GameController Build() => new(_session, _view, _pacer);

    [Fact]
    public void StartPaintsExactlyOnce()
    {
        GameController controller = Build();

        controller.Start();

        Assert.Equal(1, _view.RenderCount);
        Assert.Equal(1, _session.RendersThisSession);
    }

    [Fact]
    public void StartHandsTheViewTheStatusLine()
    {
        GameController controller = Build();

        controller.Start();

        Assert.Equal(_session.StatusText, _view.Last.StatusText);
        Assert.NotEqual(string.Empty, _view.Last.StatusText);
    }

    [Fact]
    public void NothingPaintsBeforeStart()
    {
        Build();

        Assert.Equal(0, _view.RenderCount);
    }

    [Fact]
    public void BootAndControllerPathsAreInstrumented()
    {
        GameController controller = Build();

        controller.Start();

        Assert.True(_sink.HasCategory(LogCategory.Controller), "construction emitted no Controller line");
        Assert.True(_sink.HasCategory(LogCategory.Boot), "start emitted no Boot line");
        Assert.Contains(_sink.MessagesFor(LogCategory.View), message => message.Contains("refresh", StringComparison.Ordinal));
    }

    [Fact]
    public void SilencedCategoriesEmitNothing()
    {
        Log.SetLevel(LogCategory.View, LogLevel.Off);
        GameController controller = Build();

        controller.Start();

        Assert.False(_sink.HasCategory(LogCategory.View), "View lines leaked past an Off level");
    }

    [Fact]
    public void TheShellSchedulesNoTimedWork()
    {
        GameController controller = Build();

        controller.Start();

        Assert.Same(_pacer, controller.Pacer);
        Assert.Equal(0, _pacer.PendingCount);
    }
}
