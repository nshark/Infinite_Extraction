namespace IE.Controller.Pacing;

/// <summary>
/// The controller's only view of time. The engine implementation defers to the
/// scene tree; the test implementation runs queued callbacks on demand. Injecting
/// this is what keeps timed behaviour testable without a frame loop.
/// </summary>
public interface IPacer
{
    /// <summary>Invokes <paramref name="callback"/> after a delay, in milliseconds.</summary>
    void Schedule(int delayMilliseconds, Action callback);

    /// <summary>Drops every pending callback.</summary>
    void CancelAll();
}
