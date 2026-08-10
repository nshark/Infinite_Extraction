using IE.Model;
using IE.Model.Primitives;
using Xunit;

namespace IE.Tests;

/// <summary>
/// Pure rules, tested with no engine, no view, and no frame loop -- the reason
/// gameplay logic belongs in IE.Model rather than in a node script.
/// </summary>
public sealed class ExtractionRulesTests
{
    private static ExtractionState NewState(int ore = 100, int power = 8) =>
        new(gridWidth: 4, gridHeight: 3, oreRemaining: ore, drillPower: power);

    [Fact]
    public void ExtractMovesDrillPowerWorthOfOre()
    {
        ExtractionState state = NewState(ore: 100, power: 8);

        int yield = ExtractionRules.Extract(state);

        Assert.Equal(8, yield);
        Assert.Equal(92, state.OreRemaining);
        Assert.Equal(8, state.OreExtracted);
    }

    [Fact]
    public void ExtractIsClampedByRemainingOreSoTheSeamNeverGoesNegative()
    {
        ExtractionState state = NewState(ore: 3, power: 8);

        int yield = ExtractionRules.Extract(state);

        Assert.Equal(3, yield);
        Assert.Equal(0, state.OreRemaining);
        Assert.Equal(3, state.OreExtracted);
    }

    [Fact]
    public void ExtractOnASpentSeamYieldsNothingAndChangesNothing()
    {
        ExtractionState state = NewState(ore: 0);

        Assert.False(ExtractionRules.CanExtract(state));
        Assert.Equal(0, ExtractionRules.Extract(state));
        Assert.Equal(0, state.OreExtracted);
    }

    [Fact]
    public void UpgradeDrillIsClampedToMaxDrillPower()
    {
        ExtractionState state = NewState(power: ExtractionRules.MaxDrillPower - 1);

        ExtractionRules.UpgradeDrill(state, 10);

        Assert.Equal(ExtractionRules.MaxDrillPower, state.DrillPower);
    }

    [Fact]
    public void MoveInsideTheGridSucceeds()
    {
        ExtractionState state = NewState();

        Assert.True(ExtractionRules.TryMoveDrill(state, 1, 0));
        Assert.Equal(new GridPosition(1, 0), state.DrillPosition);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(4, 0)]
    [InlineData(0, 3)]
    public void MoveOutsideTheGridIsRejectedAndLeavesTheDrillAlone(int dx, int dy)
    {
        ExtractionState state = NewState();

        Assert.False(ExtractionRules.TryMoveDrill(state, dx, dy));
        Assert.Equal(GridPosition.Origin, state.DrillPosition);
    }

    [Fact]
    public void CloneCopiesEveryFieldSoSnapshotsAreIndependent()
    {
        ExtractionState state = NewState();
        ExtractionRules.Extract(state);
        ExtractionRules.TryMoveDrill(state, 1, 1);

        ExtractionState copy = state.Clone();
        ExtractionRules.Extract(state);

        Assert.Equal(8, copy.OreExtracted);
        Assert.Equal(new GridPosition(1, 1), copy.DrillPosition);
        Assert.NotEqual(state.OreRemaining, copy.OreRemaining);
    }
}
