using System.Text.Json.Nodes;
using IE.Controller.Pacing;
using IE.Controller.Undo;
using IE.Controller.Views;
using IE.Model;
using IE.Model.Diagnostics;
using IE.Model.State;

namespace IE.Controller;

/// <summary>
/// Owns the run: turns user intent into model mutations, then repaints the view.
/// </summary>
public sealed class GameController
{
    public const int TickIntervalMs = 50;
    public const int FloorWidth = 64;
    public const int FloorHeight = 64;

    private readonly IMainView _view;
    private readonly Stack<IUndoEntry> _undoStack = new();
    private readonly Stack<IUndoEntry> _redoStack = new();

    public GameController(GameState game, SessionState session, IMainView view, IPacer pacer)
    {
        Game = game;
        Session = session;
        _view = view;
        Pacer = pacer;

        Log.Info(LogCategory.Controller, "controller created");
    }

    public GameState Game { get; }

    public SessionState Session { get; }

    public IPacer Pacer { get; }

    /// <summary>Paints the opening frame and kicks off the simulation tick loop.</summary>
    public void Start()
    {
        Log.Info(LogCategory.Boot, "controller start");
        Session.StatusText = "Ready.";
        Refresh();
        ScheduleTick();
    }

    // ---------------------------------------------------------- simulation ----

    private void ScheduleTick()
    {
        Pacer.Schedule(TickIntervalMs, OnTick);
    }

    private void OnTick()
    {
        Game.Update(1);
        Log.Trace(LogCategory.Controller, "tick");
        Refresh();
        ScheduleTick();
    }

    // ----------------------------------------------------------- placement ----

    public bool HandlePlaceAu(int x, int y, AuType auType, RecipeId recipeId, Direction facing)
    {
        JsonNode? auData = GameData.AuTypes[(int)auType];
        int width = auData!["footprintWidth"]!.GetValue<int>();
        int height = auData["footprintHeight"]!.GetValue<int>();

        if (facing is Direction.East or Direction.West)
        {
            (width, height) = (height, width);
        }

        GridPosition pos = new(x, y, width, height);
        AssemblingUnitState au = new(auType, recipeId, facing);

        if (!Game.Floor.TryPlace(pos, au))
        {
            Log.Debug(LogCategory.Controller, $"place failed at ({x},{y}) {auType}; collision");
            Refresh();
            return false;
        }

        _undoStack.Push(new PlaceAuEntry(Game.Floor, pos, au));
        _redoStack.Clear();
        Log.Info(LogCategory.Controller, $"place {auType} at ({x},{y}) {width}x{height} {facing}");
        Refresh();
        return true;
    }

    // --------------------------------------------------------------- undo ----

    public void HandleUndo()
    {
        if (_undoStack.Count == 0)
        {
            Log.Debug(LogCategory.Undo, "undo: stack empty");
            return;
        }

        IUndoEntry entry = _undoStack.Pop();
        entry.Undo();
        _redoStack.Push(entry);
        Log.Info(LogCategory.Undo, "undo");
        Refresh();
    }

    public void HandleRedo()
    {
        if (_redoStack.Count == 0)
        {
            Log.Debug(LogCategory.Undo, "redo: stack empty");
            return;
        }

        IUndoEntry entry = _redoStack.Pop();
        entry.Redo();
        _undoStack.Push(entry);
        Log.Info(LogCategory.Undo, "redo");
        Refresh();
    }

    // ----------------------------------------------------------- rotation ----

    public void HandleRotate()
    {
        Session.SelectedRotation = Session.SelectedRotation switch
        {
            Direction.North => Direction.East,
            Direction.East => Direction.South,
            Direction.South => Direction.West,
            _ => Direction.North,
        };
        Log.Debug(LogCategory.Controller, $"rotate → {Session.SelectedRotation}");
    }

    // ------------------------------------------------------------- refresh ----

    /// <summary>
    /// The single UI update path. Nothing else in this class may call into the view
    /// to repaint -- one funnel means the view can never drift from the model.
    /// </summary>
    private void Refresh()
    {
        List<PlacedItemViewModel> items = new();
        foreach (KeyValuePair<GridPosition, IBuildable> entry in Game.Floor._grid)
        {
            GridPosition pos = entry.Key;
            string typeLabel;
            Direction facing;

            if (entry.Value is AssemblingUnitState au)
            {
                typeLabel = au.GetAuType().ToString();
                facing = au.GetFacing();
            }
            else
            {
                typeLabel = "Unknown";
                facing = Direction.North;
            }

            items.Add(new PlacedItemViewModel(
                pos.X, pos.Y, pos.Width, pos.Height,
                facing, typeLabel));
        }

        MainViewModel model = new(
            StatusText: Session.StatusText,
            FloorWidth: FloorWidth,
            FloorHeight: FloorHeight,
            PlacedItems: items);

        Session.RendersThisSession++;

        if (Log.IsEnabled(LogCategory.View, LogLevel.Trace))
        {
            Log.Trace(LogCategory.View, $"refresh #{Session.RendersThisSession}; items={items.Count}");
        }

        _view.Render(model);
    }
}
