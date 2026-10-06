using Godot;
using IE.App.Platform;
using IE.Controller;
using IE.Controller.Views;
using IE.Model;
using IE.Model.Diagnostics;
using IE.Model.State;

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
    private Label _status = null!;
    private GameController _controller = null!;

    public override void _Ready()
    {
        _status = GetNode<Label>("UI/Status");

        // Instrumentation first, so the rest of boot is visible in the log.
        Log.Install(new GodotLogSink());
        Log.Configure(OS.GetEnvironment("IE_LOG"));
        Log.Info(LogCategory.Boot, $"boot; verbose-compiled={Log.VerboseCompiled}");

        GameData.Load();
        GameState game = new();
        SessionState session = new();
        _controller = new GameController(game, session, this, new EnginePacer(GetTree()));
        _controller.Start();

        if (OS.GetEnvironment("IE_HEADLESS_SELFTEST") == "1")
        {
            CallDeferred(nameof(RunSelfTest));
        }
    }

    // ------------------------------------------------------------ IMainView ---

    public void Render(MainViewModel model)
    {
        _status.Text = model.StatusText;
        Log.Trace(LogCategory.View, $"render {model}");
    }

    // ---------------------------------------------------------------- input ---

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } key)
        {
            return;
        }

        if (key.Keycode != Key.Escape)
        {
            return;
        }

        Log.Debug(LogCategory.Input, "escape pressed; quitting");
        GetViewport().SetInputAsHandled();
        GetTree().Quit();
    }

    // ------------------------------------------------------------- selftest ---

    /// <summary>
    /// Proves the engine-side wiring actually executes: boot ran, the controller
    /// reached the refresh funnel, and the real view painted. Quits on its own, so
    /// a headless run produces no teardown noise. Enabled by IE_HEADLESS_SELFTEST=1;
    /// see scripts/headless-selftest.sh. Extend it as gameplay lands.
    /// </summary>
    private void RunSelfTest()
    {
        Log.Info(LogCategory.Boot, "selftest: begin");

        int renders = _controller.Session.RendersThisSession;
        bool ok = renders > 0 && _status.Text.Length > 0;

        if (ok)
        {
            Log.Info(LogCategory.Boot, $"selftest: pass; renders={renders}; status=\"{_status.Text}\"");
        }
        else
        {
            Log.Error(LogCategory.Boot, $"selftest: FAIL; renders={renders}; status=\"{_status.Text}\"");
        }

        GetTree().Quit(ok ? 0 : 1);
    }
}
