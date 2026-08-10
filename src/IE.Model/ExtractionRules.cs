using IE.Model.Primitives;

namespace IE.Model;

/// <summary>
/// Pure extraction rules: static, framework-free, integer-only, no I/O. Every
/// gameplay decision belongs in a class like this one so it can be unit-tested
/// without an engine, a window, or a frame loop.
/// </summary>
public static class ExtractionRules
{
    public const int MaxDrillPower = 32;

    public static bool IsInBounds(ExtractionState state, GridPosition position) =>
        position.X >= 0 && position.Y >= 0 &&
        position.X < state.GridWidth && position.Y < state.GridHeight;

    public static bool CanExtract(ExtractionState state) => state.OreRemaining > 0;

    /// <summary>
    /// Ore a single extraction tick would remove -- clamped by what is left, so the
    /// seam can never go negative.
    /// </summary>
    public static int YieldFor(ExtractionState state) =>
        Math.Min(state.DrillPower, state.OreRemaining);

    /// <summary>
    /// Applies one extraction tick. Returns the ore moved, or 0 when the seam is
    /// spent. Callers funnel this through the controller, never a view.
    /// </summary>
    public static int Extract(ExtractionState state)
    {
        if (!CanExtract(state))
        {
            return 0;
        }

        int yield = YieldFor(state);
        state.OreRemaining -= yield;
        state.OreExtracted += yield;
        return yield;
    }

    /// <summary>Upgrades the drill, clamped to <see cref="MaxDrillPower"/>.</summary>
    public static void UpgradeDrill(ExtractionState state, int increment)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(increment);
        state.DrillPower = Math.Min(state.DrillPower + increment, MaxDrillPower);
    }

    public static bool TryMoveDrill(ExtractionState state, int dx, int dy)
    {
        GridPosition target = state.DrillPosition.Offset(dx, dy);
        if (!IsInBounds(state, target))
        {
            return false;
        }

        state.DrillPosition = target;
        return true;
    }
}
