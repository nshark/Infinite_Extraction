using IE.Controller;
using IE.Controller.Views;
using IE.Model;
using IE.Model.Diagnostics;
using IE.Tests.Fakes;
using Xunit;

namespace IE.Tests;

/// <summary>
/// Controller flows driven through the mock view and the synchronous pacer. These
/// cover the invariants that are easy to break by accident: one refresh per state
/// change, one undo entry per real change, none for a no-op.
/// </summary>
public sealed class GameControllerTests : IDisposable
{
    private readonly FakeMainView _view = new();
    private readonly SynchronousPacer _pacer = new();
    private readonly ExtractionState _state = new(gridWidth: 4, gridHeight: 3, oreRemaining: 100, drillPower: 8);
    private readonly SessionState _session = new();
    private readonly GameController _controller;

    public GameControllerTests()
    {
        Log.Reset();
        _controller = new GameController(_state, _session, _view, _pacer);
        _controller.Start();
    }

    public void Dispose() => Log.Reset();

    [Fact]
    public void StartPaintsTheOpeningFrame()
    {
        Assert.Equal(1, _view.RenderCount);
        Assert.Equal(100, _view.Last.OreRemaining);
        Assert.False(_view.Last.CanUndo);
    }

    [Fact]
    public void ExtractMutatesTheModelAndRepaintsExactlyOnce()
    {
        _controller.HandleExtractPressed();

        Assert.Equal(2, _view.RenderCount);
        Assert.Equal(92, _view.Last.OreRemaining);
        Assert.Equal(8, _view.Last.OreExtracted);
        Assert.Equal(new[] { 8 }, _view.FeedbackYields);
    }

    [Fact]
    public void EveryRealChangePushesExactlyOneUndoEntry()
    {
        _controller.HandleExtractPressed();
        _controller.HandleMovePressed(1, 0);

        Assert.Equal(2, _controller.UndoDepth);
        Assert.True(_view.Last.CanUndo);
    }

    [Fact]
    public void ANoOpInteractionPushesNoUndoEntry()
    {
        // Blocked by the grid edge: the handler runs, the status line changes, but
        // no snapshotted state moves -- so undo must not gain an entry.
        _controller.HandleMovePressed(-1, 0);

        Assert.Equal(0, _controller.UndoDepth);
        Assert.False(_view.Last.CanUndo);
        Assert.Equal(2, _view.RenderCount);
    }

    [Fact]
    public void UndoRestoresTheCoreStateAndTheUserIntentSlice()
    {
        _controller.HandleExtractPressed();
        _controller.HandleToggleAutoExtract();
        Assert.True(_session.AutoExtractEnabled);

        _controller.HandleUndoPressed();

        Assert.False(_session.AutoExtractEnabled);
        Assert.Equal(92, _state.OreRemaining);

        _controller.HandleUndoPressed();

        Assert.Equal(100, _state.OreRemaining);
        Assert.Equal(0, _state.OreExtracted);
        Assert.Equal(0, _controller.UndoDepth);
    }

    [Fact]
    public void UndoOnAnEmptyStackIsHarmlessAndStillRepaints()
    {
        _controller.HandleUndoPressed();

        Assert.Equal(2, _view.RenderCount);
        Assert.Equal(100, _state.OreRemaining);
    }

    [Fact]
    public void TransientSessionFieldsAreNotRestoredByUndo()
    {
        _controller.HandleExtractPressed();
        int ticksAfterExtract = _session.TicksThisSession;

        _controller.HandleUndoPressed();

        // The counter is diagnostics, not player intent: undo leaves it alone.
        Assert.Equal(ticksAfterExtract, _session.TicksThisSession);
    }

    [Fact]
    public void AnExceptionInsideAHandlerPropagatesAndPushesNothing()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _controller.HandleUpgradePressed(-1));

        Assert.Equal(0, _controller.UndoDepth);
    }

    [Fact]
    public void EnablingAutoExtractSchedulesATickAtTheConfiguredInterval()
    {
        _controller.HandleToggleAutoExtract();

        Assert.Equal(1, _pacer.PendingCount);
        Assert.Equal(new[] { GameController.AutoExtractIntervalMs }, _pacer.ScheduledDelays);
    }

    [Fact]
    public void EachAutoTickExtractsAndQueuesTheNextOne()
    {
        _controller.HandleToggleAutoExtract();

        Assert.True(_pacer.RunNext());

        Assert.Equal(92, _state.OreRemaining);
        Assert.Equal(1, _pacer.PendingCount);
    }

    [Fact]
    public void AutoExtractStopsItselfWhenTheSeamRunsOut()
    {
        ExtractionState nearlyEmpty = new(gridWidth: 4, gridHeight: 3, oreRemaining: 8, drillPower: 8);
        SessionState session = new();
        FakeMainView view = new();
        SynchronousPacer pacer = new();
        GameController controller = new(nearlyEmpty, session, view, pacer);
        controller.Start();

        controller.HandleToggleAutoExtract();
        pacer.RunUpTo(maxTicks: 10);

        Assert.False(session.AutoExtractEnabled);
        Assert.Equal(0, nearlyEmpty.OreRemaining);
        Assert.Equal(0, pacer.PendingCount);
    }

    [Fact]
    public void DisablingAutoExtractDropsPendingTicks()
    {
        _controller.HandleToggleAutoExtract();
        _controller.HandleToggleAutoExtract();

        Assert.Equal(0, _pacer.PendingCount);
        Assert.False(_session.AutoExtractEnabled);
    }

    [Fact]
    public void TheViewOnlyEverReceivesPrimitives()
    {
        // Guards the "views never mutate the model" rule at the type level: if the
        // view model ever gains a reference to live state, this stops compiling.
        MainViewModel model = _view.Last;

        Assert.IsType<int>(model.OreRemaining);
        Assert.IsType<string>(model.StatusText);
        Assert.IsType<bool>(model.CanUndo);
    }

    [Fact]
    public void InstrumentationProvesTheControllerPathRanWithoutLeakingOtherCategories()
    {
        RecordingLogSink sink = new();
        Log.Reset();
        Log.Install(sink);
        Log.Configure("Controller:info");

        GameController controller = new(
            new ExtractionState(4, 3, 100, 8), new SessionState(), new FakeMainView(), new SynchronousPacer());
        controller.Start();
        controller.HandleToggleAutoExtract();

        Assert.True(sink.HasCategory(LogCategory.Controller));
        Assert.Contains(sink.MessagesFor(LogCategory.Controller), message => message.Contains("auto-extract=True"));

        // Correct gating: categories nobody turned up stay silent.
        Assert.False(sink.HasCategory(LogCategory.View));
        Assert.False(sink.HasCategory(LogCategory.Undo));
        Assert.False(sink.HasCategory(LogCategory.Boot));
    }
}
