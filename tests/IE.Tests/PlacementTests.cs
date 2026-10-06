using IE.Controller;
using IE.Controller.Views;
using IE.Model;
using IE.Model.Diagnostics;
using IE.Model.State;
using IE.Tests.Fakes;
using Xunit;

namespace IE.Tests;

public sealed class PlacementTests : IDisposable
{
    private readonly RecordingLogSink _sink = new();
    private readonly FakeMainView _view = new();
    private readonly SynchronousPacer _pacer = new();
    private readonly SessionState _session = new();
    private readonly GameState _game = new();

    public PlacementTests()
    {
        Log.Reset();
        Log.Install(_sink);
        GameData.Load();
        Log.SetLevel(LogCategory.Controller, LogLevel.Trace);
        Log.SetLevel(LogCategory.Undo, LogLevel.Trace);
    }

    public void Dispose() => Log.Reset();

    private GameController Build()
    {
        GameController controller = new(_game, _session, _view, _pacer);
        controller.Start();
        return controller;
    }

    [Fact]
    public void PlaceOnEmptyFloorSucceeds()
    {
        GameController controller = Build();

        bool placed = controller.HandlePlaceAu(0, 0, AuType.Smelter, RecipeId.SmeltScrapToIron, Direction.North);

        Assert.True(placed);
        Assert.Single(_view.Last.PlacedItems);
    }

    [Fact]
    public void PlacedItemHasCorrectFootprint()
    {
        GameController controller = Build();

        controller.HandlePlaceAu(3, 5, AuType.Smelter, RecipeId.SmeltScrapToIron, Direction.East);

        PlacedItemViewModel item = _view.Last.PlacedItems[0];
        Assert.Equal(3, item.X);
        Assert.Equal(5, item.Y);
        Assert.Equal(2, item.Width);
        Assert.Equal(2, item.Height);
        Assert.Equal(Direction.East, item.Rotation);
        Assert.Equal("Smelter", item.TypeLabel);
    }

    [Fact]
    public void PlaceOnOccupiedFails()
    {
        GameController controller = Build();
        controller.HandlePlaceAu(0, 0, AuType.Smelter, RecipeId.SmeltScrapToIron, Direction.North);

        bool second = controller.HandlePlaceAu(1, 1, AuType.Smelter, RecipeId.SmeltScrapToIron, Direction.North);

        Assert.False(second);
        Assert.Single(_view.Last.PlacedItems);
    }

    [Fact]
    public void PlaceAdjacentSucceeds()
    {
        GameController controller = Build();
        controller.HandlePlaceAu(0, 0, AuType.Smelter, RecipeId.SmeltScrapToIron, Direction.North);

        bool second = controller.HandlePlaceAu(2, 0, AuType.Smelter, RecipeId.SmeltScrapToIron, Direction.North);

        Assert.True(second);
        Assert.Equal(2, _view.Last.PlacedItems.Count);
    }

    [Fact]
    public void UndoReversesPlacement()
    {
        GameController controller = Build();
        controller.HandlePlaceAu(0, 0, AuType.Smelter, RecipeId.SmeltScrapToIron, Direction.North);

        controller.HandleUndo();

        Assert.Empty(_view.Last.PlacedItems);
    }

    [Fact]
    public void RedoRestoresPlacement()
    {
        GameController controller = Build();
        controller.HandlePlaceAu(0, 0, AuType.Smelter, RecipeId.SmeltScrapToIron, Direction.North);
        controller.HandleUndo();

        controller.HandleRedo();

        Assert.Single(_view.Last.PlacedItems);
    }

    [Fact]
    public void MultipleUndosReverseInOrder()
    {
        GameController controller = Build();
        controller.HandlePlaceAu(0, 0, AuType.Smelter, RecipeId.SmeltScrapToIron, Direction.North);
        controller.HandlePlaceAu(4, 4, AuType.Smelter, RecipeId.SmeltScrapToCopper, Direction.South);
        Assert.Equal(2, _view.Last.PlacedItems.Count);

        controller.HandleUndo();
        Assert.Single(_view.Last.PlacedItems);
        Assert.Equal(0, _view.Last.PlacedItems[0].X);

        controller.HandleUndo();
        Assert.Empty(_view.Last.PlacedItems);
    }

    [Fact]
    public void NewActionClearsRedo()
    {
        GameController controller = Build();
        controller.HandlePlaceAu(0, 0, AuType.Smelter, RecipeId.SmeltScrapToIron, Direction.North);
        controller.HandleUndo();

        controller.HandlePlaceAu(4, 4, AuType.Smelter, RecipeId.SmeltScrapToCopper, Direction.South);
        controller.HandleRedo();

        Assert.Single(_view.Last.PlacedItems);
        Assert.Equal(4, _view.Last.PlacedItems[0].X);
    }

    [Fact]
    public void FailedPlacementDoesNotPushUndo()
    {
        GameController controller = Build();
        controller.HandlePlaceAu(0, 0, AuType.Smelter, RecipeId.SmeltScrapToIron, Direction.North);
        controller.HandlePlaceAu(1, 1, AuType.Smelter, RecipeId.SmeltScrapToIron, Direction.North);

        controller.HandleUndo();

        Assert.Empty(_view.Last.PlacedItems);
    }

    [Fact]
    public void UndoOnEmptyStackIsNoOp()
    {
        GameController controller = Build();
        int rendersBefore = _view.RenderCount;

        controller.HandleUndo();

        Assert.Equal(rendersBefore, _view.RenderCount);
    }

    [Fact]
    public void RedoOnEmptyStackIsNoOp()
    {
        GameController controller = Build();
        int rendersBefore = _view.RenderCount;

        controller.HandleRedo();

        Assert.Equal(rendersBefore, _view.RenderCount);
    }

    [Fact]
    public void PlacementIsInstrumented()
    {
        GameController controller = Build();

        controller.HandlePlaceAu(3, 7, AuType.Smelter, RecipeId.SmeltScrapToIron, Direction.North);

        Assert.Contains(
            _sink.MessagesFor(LogCategory.Controller),
            m => m.Contains("place", StringComparison.OrdinalIgnoreCase) && m.Contains("3") && m.Contains("7"));
    }
}
