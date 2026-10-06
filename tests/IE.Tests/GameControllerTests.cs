using IE.Controller;
using IE.Controller.Views;
using IE.Model;
using IE.Model.Diagnostics;
using IE.Model.State;
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
    private readonly GameState _game = new();

    public GameControllerTests()
    {
        Log.Reset();
        Log.Install(_sink);
        GameData.Load();
        Log.SetLevel(LogCategory.Boot, LogLevel.Trace);
        Log.SetLevel(LogCategory.Controller, LogLevel.Trace);
        Log.SetLevel(LogCategory.View, LogLevel.Trace);
    }

    public void Dispose() => Log.Reset();

    private GameController Build() => new(_game, _session, _view, _pacer);

    [Fact]
    public void GameDataIsAccessibleAfterLoad()
    {
        Assert.True(GameData.IsLoaded);
        Assert.True(GameData.Recipes.Count > 0);
    }

    [Fact]
    public void ControllerHoldsTheGameState()
    {
        GameController controller = Build();

        Assert.Same(_game, controller.Game);
        Assert.NotNull(controller.Game.Floor);
    }

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
    public void StartSchedulesFirstTick()
    {
        GameController controller = Build();

        controller.Start();

        Assert.Same(_pacer, controller.Pacer);
        Assert.Equal(1, _pacer.PendingCount);
        Assert.Equal(GameController.TickIntervalMs, _pacer.ScheduledDelays[^1]);
    }

    [Fact]
    public void RunningOneTickAdvancesAndReschedules()
    {
        GameController controller = Build();
        controller.Start();
        int rendersAfterStart = _view.RenderCount;

        _pacer.RunNext();

        Assert.Equal(rendersAfterStart + 1, _view.RenderCount);
        Assert.Equal(1, _pacer.PendingCount);
    }

    [Fact]
    public void RunningMultipleTicksAccumulates()
    {
        GameController controller = Build();
        controller.Start();

        int ran = _pacer.RunUpTo(5);

        Assert.Equal(5, ran);
        Assert.Equal(6, _view.RenderCount);
    }

    [Fact]
    public void CancelAllStopsTheLoop()
    {
        GameController controller = Build();
        controller.Start();
        Assert.Equal(1, _pacer.PendingCount);

        _pacer.CancelAll();

        Assert.Equal(0, _pacer.PendingCount);
    }

    [Fact]
    public void EmptyFloorProducesEmptyPlacedItems()
    {
        GameController controller = Build();

        controller.Start();

        Assert.Empty(_view.Last.PlacedItems);
    }

    [Fact]
    public void FloorDimensionsArePopulated()
    {
        GameController controller = Build();

        controller.Start();

        Assert.Equal(GameController.FloorWidth, _view.Last.FloorWidth);
        Assert.Equal(GameController.FloorHeight, _view.Last.FloorHeight);
    }

    [Fact]
    public void PlacedItemAppearsInViewModel()
    {
        GameController controller = Build();
        controller.Start();
        GridPosition pos = new(2, 3, 2, 2, Direction.North);
        AssemblingUnitState au = new(AuType.Smelter, RecipeId.SmeltScrapToIron);
        _game.Floor.TryPlace(pos, au);

        _pacer.RunNext();

        Assert.Single(_view.Last.PlacedItems);
        PlacedItemViewModel item = _view.Last.PlacedItems[0];
        Assert.Equal(2, item.X);
        Assert.Equal(3, item.Y);
        Assert.Equal(2, item.Width);
        Assert.Equal(2, item.Height);
        Assert.Equal(Direction.North, item.Rotation);
        Assert.Equal("Smelter", item.TypeLabel);
    }
}
