using IE.Model;
using IE.Model.Primitives;

namespace IE.Controller;

/// <summary>
/// One undo entry: the full core state plus the user-intent slice of session state.
/// Transient session fields and the undo stack itself stay out -- restoring a status
/// string or a diagnostics counter is not what the player meant by "undo".
/// </summary>
public sealed class Snapshot
{
    private Snapshot(ExtractionState core, bool autoExtractEnabled)
    {
        Core = core;
        AutoExtractEnabled = autoExtractEnabled;
    }

    public ExtractionState Core { get; }

    public bool AutoExtractEnabled { get; }

    public static Snapshot Capture(ExtractionState core, SessionState session) =>
        new(core.Clone(), session.AutoExtractEnabled);

    /// <summary>Writes this snapshot back over the live state.</summary>
    public void RestoreInto(ExtractionState core, SessionState session)
    {
        core.OreRemaining = Core.OreRemaining;
        core.OreExtracted = Core.OreExtracted;
        core.DrillPower = Core.DrillPower;
        core.DrillPosition = Core.DrillPosition;
        session.AutoExtractEnabled = AutoExtractEnabled;
    }

    /// <summary>
    /// Value equality across everything a snapshot covers. Used to de-dupe no-op
    /// interactions: a handler that changed nothing must not push an undo entry.
    /// </summary>
    public bool Matches(Snapshot other)
    {
        GridPosition mine = Core.DrillPosition;
        GridPosition theirs = other.Core.DrillPosition;

        return Core.OreRemaining == other.Core.OreRemaining
            && Core.OreExtracted == other.Core.OreExtracted
            && Core.DrillPower == other.Core.DrillPower
            && mine == theirs
            && AutoExtractEnabled == other.AutoExtractEnabled;
    }
}
