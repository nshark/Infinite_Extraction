using IE.Controller.Pacing;
using IE.Controller.Views;
using IE.Model.Diagnostics;

namespace IE.Controller;

/// <summary>
/// Owns the run: turns user intent into model mutations, then repaints the view.
/// Currently a shell -- there is no gameplay to orchestrate yet, so it boots, paints
/// once, and holds the two injected boundaries that future handlers will use.
///
/// Three rules shape everything here, and all three are load-bearing:
///   * it never touches an engine type -- views arrive as <see cref="IMainView"/>,
///     time arrives as <see cref="IPacer"/>, so the whole class is unit-testable;
///   * every handler ends in exactly one <see cref="Refresh"/> call, so there is a
///     single UI update path and no drift between them;
///   * every top-level handler runs inside a tracking helper that pushes one undo
///     entry iff state actually changed -- to be reintroduced with the first
///     mutating handler.
/// </summary>
public sealed class GameController
{
    private readonly IMainView _view;

    public GameController(SessionState session, IMainView view, IPacer pacer)
    {
        Session = session;
        _view = view;
        Pacer = pacer;

        Log.Info(LogCategory.Controller, "controller created");
    }

    public SessionState Session { get; }

    /// <summary>
    /// The controller's only view of time. Exposed rather than hidden because the
    /// shell schedules nothing yet; timed behaviour goes through this and never
    /// through an engine timer.
    /// </summary>
    public IPacer Pacer { get; }

    /// <summary>Paints the opening frame. Call once, after construction.</summary>
    public void Start()
    {
        Log.Info(LogCategory.Boot, "controller start");
        Session.StatusText = "Ready.";
        Refresh();
    }

    // ------------------------------------------------------------- refresh ----

    /// <summary>
    /// The single UI update path. Nothing else in this class may call into the view
    /// to repaint -- one funnel means the view can never drift from the model.
    /// </summary>
    private void Refresh()
    {
        MainViewModel model = new(StatusText: Session.StatusText);
        Session.RendersThisSession++;

        if (Log.IsEnabled(LogCategory.View, LogLevel.Trace))
        {
            // Guarded so the interpolation cost is only paid when it is wanted.
            Log.Trace(LogCategory.View, $"refresh #{Session.RendersThisSession} {model}");
        }

        _view.Render(model);
    }
}
