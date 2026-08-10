using IE.Controller.Pacing;
using IE.Controller.Views;
using IE.Model;
using IE.Model.Diagnostics;

namespace IE.Controller;

/// <summary>
/// Owns the run: turns user intent into model mutations, then repaints the view.
///
/// Three rules shape everything here, and all three are load-bearing:
///   * it never touches an engine type -- views arrive as <see cref="IMainView"/>,
///     time arrives as <see cref="IPacer"/>, so the whole class is unit-testable;
///   * every handler ends in exactly one <see cref="Refresh"/> call, so there is a
///     single UI update path and no drift between them;
///   * every top-level handler runs inside <see cref="Track"/>, which pushes one
///     undo entry if and only if state actually changed.
/// </summary>
public sealed class GameController
{
    /// <summary>Gap between automatic extraction ticks, in milliseconds.</summary>
    public const int AutoExtractIntervalMs = 500;

    private readonly IMainView _view;
    private readonly IPacer _pacer;
    private readonly Stack<Snapshot> _undoStack = new();

    private bool _autoTickScheduled;

    public GameController(ExtractionState state, SessionState session, IMainView view, IPacer pacer)
    {
        State = state;
        Session = session;
        _view = view;
        _pacer = pacer;

        Log.Info(LogCategory.Controller, $"controller created; {state}");
    }

    public ExtractionState State { get; }

    public SessionState Session { get; }

    public int UndoDepth => _undoStack.Count;

    /// <summary>Paints the opening frame. Call once, after construction.</summary>
    public void Start()
    {
        Log.Info(LogCategory.Boot, "controller start");
        Session.StatusText = "Ready to drill.";
        Refresh();
    }

    // ------------------------------------------------------------- handlers ---

    public void HandleExtractPressed() => Track(nameof(HandleExtractPressed), () =>
    {
        int yield = ExtractionRules.Extract(State);
        Session.TicksThisSession++;

        if (yield == 0)
        {
            Session.StatusText = "Seam is spent.";
            Log.Info(LogCategory.Controller, "extract requested but seam is empty");
            return;
        }

        Session.StatusText = $"Extracted {yield}.";
        Log.Debug(LogCategory.Model, $"extract yield={yield}; {State}");
        _view.PlayExtractionFeedback(yield);
    });

    public void HandleMovePressed(int dx, int dy) => Track(nameof(HandleMovePressed), () =>
    {
        if (!ExtractionRules.TryMoveDrill(State, dx, dy))
        {
            Session.StatusText = "Blocked by the rig edge.";
            Log.Debug(LogCategory.Input, $"move ({dx},{dy}) rejected from {State.DrillPosition}");
            return;
        }

        Session.StatusText = $"Drill at {State.DrillPosition}.";
        Log.Debug(LogCategory.Input, $"move ({dx},{dy}) -> {State.DrillPosition}");
    });

    public void HandleUpgradePressed(int increment) => Track(nameof(HandleUpgradePressed), () =>
    {
        ExtractionRules.UpgradeDrill(State, increment);
        Session.StatusText = $"Drill power {State.DrillPower}.";
    });

    public void HandleToggleAutoExtract() => Track(nameof(HandleToggleAutoExtract), () =>
    {
        Session.AutoExtractEnabled = !Session.AutoExtractEnabled;
        Session.StatusText = Session.AutoExtractEnabled ? "Auto-drill engaged." : "Auto-drill off.";
        Log.Info(LogCategory.Controller, $"auto-extract={Session.AutoExtractEnabled}");

        if (Session.AutoExtractEnabled)
        {
            ScheduleAutoTick();
        }
        else
        {
            _pacer.CancelAll();
            _autoTickScheduled = false;
        }
    });

    /// <summary>
    /// Undo is not itself an undoable action, so it bypasses <see cref="Track"/> --
    /// but it still ends in the one shared refresh call.
    /// </summary>
    public void HandleUndoPressed()
    {
        if (_undoStack.Count == 0)
        {
            Session.StatusText = "Nothing to undo.";
            Log.Debug(LogCategory.Undo, "undo requested with empty stack");
            Refresh();
            return;
        }

        Snapshot previous = _undoStack.Pop();
        previous.RestoreInto(State, Session);
        Session.StatusText = "Undone.";
        Log.Info(LogCategory.Undo, $"undo applied; depth={_undoStack.Count}; {State}");
        Refresh();
    }

    // ---------------------------------------------------------- undo + tick ---

    /// <summary>
    /// Captures pre-state, runs the handler body, and pushes a single undo entry iff
    /// state actually changed. An exception inside the body propagates and nothing
    /// is pushed -- a half-mutated state has already broken the invariants, so crash
    /// rather than paper over it.
    /// </summary>
    private void Track(string handlerName, Action body)
    {
        Snapshot before = Snapshot.Capture(State, Session);

        body();

        Snapshot after = Snapshot.Capture(State, Session);
        if (before.Matches(after))
        {
            Log.Trace(LogCategory.Undo, $"{handlerName}: no state change, no undo entry");
        }
        else
        {
            _undoStack.Push(before);
            Log.Debug(LogCategory.Undo, $"{handlerName}: pushed undo entry; depth={_undoStack.Count}");
        }

        Refresh();
    }

    private void ScheduleAutoTick()
    {
        if (_autoTickScheduled)
        {
            return;
        }

        _autoTickScheduled = true;
        _pacer.Schedule(AutoExtractIntervalMs, OnAutoTick);
    }

    private void OnAutoTick()
    {
        _autoTickScheduled = false;

        if (!Session.AutoExtractEnabled)
        {
            return;
        }

        if (!ExtractionRules.CanExtract(State))
        {
            Session.AutoExtractEnabled = false;
            Session.StatusText = "Auto-drill stopped: seam is spent.";
            Log.Info(LogCategory.Controller, "auto-extract stopped, seam empty");
            Refresh();
            return;
        }

        HandleExtractPressed();
        ScheduleAutoTick();
    }

    // ------------------------------------------------------------- refresh ----

    /// <summary>
    /// The single UI update path. Nothing else in this class may call into the view
    /// to repaint -- one funnel means the view can never drift from the model.
    /// </summary>
    private void Refresh()
    {
        MainViewModel model = new(
            OreRemaining: State.OreRemaining,
            OreExtracted: State.OreExtracted,
            DrillPower: State.DrillPower,
            DrillX: State.DrillPosition.X,
            DrillY: State.DrillPosition.Y,
            CanExtract: ExtractionRules.CanExtract(State),
            AutoExtractEnabled: Session.AutoExtractEnabled,
            CanUndo: _undoStack.Count > 0,
            StatusText: Session.StatusText);

        if (Log.IsEnabled(LogCategory.View, LogLevel.Trace))
        {
            // Guarded so the interpolation cost is only paid when it is wanted.
            Log.Trace(LogCategory.View, $"refresh {model}");
        }

        _view.Render(model);
    }
}
