namespace IE.Controller;

/// <summary>
/// Per-session state that is not part of the core model. Split deliberately into
/// two kinds of field, because only one kind belongs in a snapshot once an undo
/// stack exists again:
///
///   * user-intent  -- the player chose it, so undo must restore it
///   * transient    -- derived or cosmetic, so undo must ignore it
///
/// The shell has only transient fields; user-intent ones arrive with gameplay.
/// </summary>
public sealed class SessionState
{
    // ---- transient (never snapshotted) ----

    /// <summary>Cosmetic status line. Recomputed on every refresh.</summary>
    public string StatusText { get; set; } = string.Empty;

    /// <summary>
    /// Diagnostics counter for this process only: how many times the single refresh
    /// funnel has painted. Exists so a headless run can prove the path executed.
    /// </summary>
    public int RendersThisSession { get; set; }
}
