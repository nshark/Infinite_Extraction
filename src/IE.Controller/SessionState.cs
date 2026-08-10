namespace IE.Controller;

/// <summary>
/// Per-session state that is not part of the core model. Split deliberately into
/// two kinds of field, because only one kind belongs in a snapshot:
///
///   * user-intent  -- the player chose it, so undo must restore it
///   * transient    -- derived or cosmetic, so undo must ignore it
/// </summary>
public sealed class SessionState
{
    // ---- user intent (snapshotted) ----

    /// <summary>Player asked the drill to keep running on its own.</summary>
    public bool AutoExtractEnabled { get; set; }

    // ---- transient (never snapshotted) ----

    /// <summary>Cosmetic status line. Recomputed on every refresh.</summary>
    public string StatusText { get; set; } = string.Empty;

    /// <summary>Diagnostics counter for this process only.</summary>
    public int TicksThisSession { get; set; }
}
