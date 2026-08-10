using Godot;
using IE.App.Platform;
using IE.Controller;
using IE.Controller.Views;
using IE.Model;
using IE.Model.Diagnostics;
using IE.Model.Primitives;
using IE.ViewMath;

namespace IE.App;

/// <summary>
/// The composition root and the one implementation of <see cref="IMainView"/>.
///
/// This class is framework-side and therefore NOT unit-tested: it wires the engine
/// to the core and paints what it is given. It reads state and renders -- it never
/// writes to the model. Input is forwarded to the controller, which owns mutation.
/// </summary>
public partial class MainScene : Node2D, IMainView
{
    private const int StartingOre = 240;
    private const int StartingDrillPower = 8;
    private const int GridColumns = 12;
    private const int GridRows = 8;
    private const int UpgradeIncrement = 2;

    private ColorRect _drill = null!;
    private Label _status = null!;
    private Label _readout = null!;
    private GameController _controller = null!;

    public override void _Ready()
    {
        _drill = GetNode<ColorRect>("Drill");
        _status = GetNode<Label>("UI/Status");
        _readout = GetNode<Label>("UI/Readout");

        // Instrumentation first, so the rest of boot is visible in the log.
        Log.Install(new GodotLogSink());
        Log.Configure(OS.GetEnvironment("IE_LOG"));
        Log.Info(LogCategory.Boot, $"boot; verbose-compiled={Log.VerboseCompiled}");

        ExtractionState state = new(GridColumns, GridRows, StartingOre, StartingDrillPower);
        SessionState session = new();
        _controller = new GameController(state, session, this, new EnginePacer(GetTree()));
        _controller.Start();

        _drill.Size = new Vector2(PixelLayout.CellSizePixels, PixelLayout.CellSizePixels);

        if (OS.GetEnvironment("IE_HEADLESS_SELFTEST") == "1")
        {
            CallDeferred(nameof(RunSelfTest));
        }
    }

    // ------------------------------------------------------------ IMainView ---

    public void Render(MainViewModel model)
    {
        _status.Text = model.StatusText;
        _readout.Text =
            $"ore {model.OreRemaining}    banked {model.OreExtracted}    " +
            $"power {model.DrillPower}    auto {(model.AutoExtractEnabled ? "on" : "off")}" +
            $"    undo {(model.CanUndo ? "available" : "empty")}";

        (float x, float y) = PixelLayout.CellTopLeft(new GridPosition(model.DrillX, model.DrillY));
        _drill.Position = new Vector2(x, y);
        _drill.Modulate = model.CanExtract ? Colors.White : new Color(1, 1, 1, 0.35f);
    }

    public void PlayExtractionFeedback(int yield)
    {
        Log.Trace(LogCategory.View, $"extraction feedback yield={yield}");
        _drill.Modulate = Colors.White;
    }

    // ---------------------------------------------------------------- input ---

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } key)
        {
            return;
        }

        switch (key.Keycode)
        {
            case Key.Space: _controller.HandleExtractPressed(); break;
            case Key.W: _controller.HandleMovePressed(0, -1); break;
            case Key.S: _controller.HandleMovePressed(0, 1); break;
            case Key.A: _controller.HandleMovePressed(-1, 0); break;
            case Key.D: _controller.HandleMovePressed(1, 0); break;
            case Key.E: _controller.HandleUpgradePressed(UpgradeIncrement); break;
            case Key.Tab: _controller.HandleToggleAutoExtract(); break;
            case Key.U: _controller.HandleUndoPressed(); break;
            case Key.Escape: GetTree().Quit(); break;
            default: return;
        }

        GetViewport().SetInputAsHandled();
    }

    // ------------------------------------------------------------- selftest ---

    /// <summary>
    /// Drives one scripted interaction against the REAL views and quits on its own,
    /// so a headless run proves the engine-side wiring actually executes. Enabled by
    /// IE_HEADLESS_SELFTEST=1; see scripts/headless-selftest.sh.
    /// </summary>
    private void RunSelfTest()
    {
        Log.Info(LogCategory.Boot, "selftest: begin");

        _controller.HandleExtractPressed();
        _controller.HandleMovePressed(1, 0);
        _controller.HandleUpgradePressed(UpgradeIncrement);
        _controller.HandleUndoPressed();

        bool ok = _controller.State.OreExtracted > 0 && _controller.UndoDepth > 0;
        if (ok)
        {
            Log.Info(LogCategory.Boot, $"selftest: pass; {_controller.State}; undo depth {_controller.UndoDepth}");
        }
        else
        {
            Log.Error(LogCategory.Boot, $"selftest: FAIL; {_controller.State}; undo depth {_controller.UndoDepth}");
        }

        GetTree().Quit(ok ? 0 : 1);
    }
}
