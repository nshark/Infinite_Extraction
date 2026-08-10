using IE.Model.Primitives;

namespace IE.Model;

/// <summary>
/// The authoritative core state of a run. Plain data plus the minimum invariants;
/// all decision logic lives in the static rules classes so it stays testable.
///
/// Mutated only by the controller layer. Views read it and never write to it.
/// </summary>
public sealed class ExtractionState
{
    public ExtractionState(int gridWidth, int gridHeight, int oreRemaining, int drillPower)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(gridWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(gridHeight);
        ArgumentOutOfRangeException.ThrowIfNegative(oreRemaining);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(drillPower);

        GridWidth = gridWidth;
        GridHeight = gridHeight;
        OreRemaining = oreRemaining;
        DrillPower = drillPower;
        DrillPosition = GridPosition.Origin;
    }

    public int GridWidth { get; }

    public int GridHeight { get; }

    /// <summary>Ore left in the seam. Never negative.</summary>
    public int OreRemaining { get; set; }

    /// <summary>Ore banked so far this run.</summary>
    public int OreExtracted { get; set; }

    /// <summary>Ore removed per extraction tick.</summary>
    public int DrillPower { get; set; }

    public GridPosition DrillPosition { get; set; }

    /// <summary>Deep copy, for the undo stack. Add every new field here.</summary>
    public ExtractionState Clone() =>
        new(GridWidth, GridHeight, OreRemaining, DrillPower)
        {
            OreExtracted = OreExtracted,
            DrillPosition = DrillPosition,
        };

    public override string ToString() =>
        $"ore={OreRemaining} banked={OreExtracted} power={DrillPower} drill={DrillPosition}";
}
