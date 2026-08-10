using IE.Controller.Pacing;

namespace IE.Tests.Fakes;

/// <summary>
/// Time under test control. Callbacks queue instead of firing, so a test decides
/// exactly how many ticks elapse -- no frame loop, no sleeping, no flake.
/// </summary>
public sealed class SynchronousPacer : IPacer
{
    private readonly Queue<Action> _pending = new();

    public List<int> ScheduledDelays { get; } = new();

    public int PendingCount => _pending.Count;

    public void Schedule(int delayMilliseconds, Action callback)
    {
        ScheduledDelays.Add(delayMilliseconds);
        _pending.Enqueue(callback);
    }

    public void CancelAll() => _pending.Clear();

    /// <summary>Fires the next queued callback. Returns false when none is pending.</summary>
    public bool RunNext()
    {
        if (_pending.Count == 0)
        {
            return false;
        }

        _pending.Dequeue().Invoke();
        return true;
    }

    /// <summary>Fires up to <paramref name="maxTicks"/> callbacks, including any they queue.</summary>
    public int RunUpTo(int maxTicks)
    {
        int ran = 0;
        while (ran < maxTicks && RunNext())
        {
            ran++;
        }

        return ran;
    }
}
