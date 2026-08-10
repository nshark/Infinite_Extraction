using Godot;
using IE.Controller.Pacing;

namespace IE.App.Platform;

/// <summary>
/// The live <see cref="IPacer"/>: defers to the scene tree's timers. The controller
/// only ever sees the interface, so the same flows run against the synchronous test
/// pacer with no frame loop at all.
/// </summary>
public sealed class EnginePacer : IPacer
{
    private readonly SceneTree _tree;
    private readonly List<SceneTreeTimer> _pending = new();

    public EnginePacer(SceneTree tree) => _tree = tree;

    public void Schedule(int delayMilliseconds, Action callback)
    {
        SceneTreeTimer timer = _tree.CreateTimer(delayMilliseconds / 1000.0);
        _pending.Add(timer);
        timer.Timeout += () =>
        {
            _pending.Remove(timer);
            callback();
        };
    }

    public void CancelAll()
    {
        // A SceneTreeTimer cannot be cancelled outright; dropping the reference and
        // letting the controller ignore a stale tick is the supported pattern.
        _pending.Clear();
    }
}
