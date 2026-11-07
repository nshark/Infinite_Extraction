using IE.Model;
using IE.Model.State;
using Xunit;

namespace IE.Tests;

public sealed class PathNetworkTests
{
    private readonly PathNetwork _net = new();

    private void PlaceBelt(int x, int y, Direction dir)
    {
        _net.Place(new GridPosition(x, y), new PathComponent(PathComponentType.Belt, dir));
    }

    private bool CollidesAt(int x, int y)
    {
        return _net.Collides(new GridPosition(x, y));
    }

    // ---- Placement: single belt ----

    [Fact]
    public void PlaceSingleBeltCollidesWithOwnCell()
    {
        PlaceBelt(5, 5, Direction.East);

        Assert.True(CollidesAt(5, 5));
    }

    [Fact]
    public void PlaceSingleBeltDoesNotCollideWithAdjacentCell()
    {
        PlaceBelt(5, 5, Direction.East);

        Assert.False(CollidesAt(6, 5));
        Assert.False(CollidesAt(4, 5));
        Assert.False(CollidesAt(5, 6));
        Assert.False(CollidesAt(5, 4));
    }

    [Theory]
    [InlineData(Direction.North)]
    [InlineData(Direction.East)]
    [InlineData(Direction.South)]
    [InlineData(Direction.West)]
    public void PlaceSingleBeltEachDirection(Direction dir)
    {
        PlaceBelt(5, 5, dir);

        Assert.True(CollidesAt(5, 5));
    }

    // ---- Placement: extension ----

    [Fact]
    public void PlaceAdjacentSameDirectionExtendsForward()
    {
        PlaceBelt(5, 5, Direction.East);
        PlaceBelt(6, 5, Direction.East);

        Assert.True(CollidesAt(5, 5));
        Assert.True(CollidesAt(6, 5));
    }

    [Fact]
    public void PlaceAdjacentSameDirectionExtendsPrepend()
    {
        PlaceBelt(6, 5, Direction.East);
        PlaceBelt(5, 5, Direction.East);

        Assert.True(CollidesAt(5, 5));
        Assert.True(CollidesAt(6, 5));
    }

    [Fact]
    public void ExtendedPathCollidesWithAllCells()
    {
        for (int x = 3; x <= 7; x++)
            PlaceBelt(x, 5, Direction.East);

        for (int x = 3; x <= 7; x++)
            Assert.True(CollidesAt(x, 5));

        Assert.False(CollidesAt(2, 5));
        Assert.False(CollidesAt(8, 5));
    }

    [Fact]
    public void AdjacentDifferentDirectionDoesNotMerge()
    {
        PlaceBelt(5, 5, Direction.East);
        PlaceBelt(6, 5, Direction.North);

        Assert.True(CollidesAt(5, 5));
        Assert.True(CollidesAt(6, 5));

        _net.Destroy(5, 5);

        Assert.False(CollidesAt(5, 5));
        Assert.True(CollidesAt(6, 5));
    }

    [Fact]
    public void ParallelSameDirectionDoesNotMerge()
    {
        PlaceBelt(5, 5, Direction.East);
        PlaceBelt(5, 6, Direction.East);

        _net.Destroy(5, 5);

        Assert.False(CollidesAt(5, 5));
        Assert.True(CollidesAt(5, 6));
    }

    [Fact]
    public void NonAdjacentSameDirectionDoesNotMerge()
    {
        PlaceBelt(5, 5, Direction.East);
        PlaceBelt(8, 5, Direction.East);

        _net.Destroy(5, 5);

        Assert.False(CollidesAt(5, 5));
        Assert.True(CollidesAt(8, 5));
    }

    // ---- Placement: merge (gap fill) ----

    [Fact]
    public void FillGapBetweenTwoPathsMerges()
    {
        PlaceBelt(5, 5, Direction.East);
        PlaceBelt(7, 5, Direction.East);
        PlaceBelt(6, 5, Direction.East);

        Assert.True(CollidesAt(5, 5));
        Assert.True(CollidesAt(6, 5));
        Assert.True(CollidesAt(7, 5));

        _net.Destroy(5, 5);

        Assert.False(CollidesAt(5, 5));
        Assert.True(CollidesAt(6, 5));
        Assert.True(CollidesAt(7, 5));
    }

    [Theory]
    [InlineData(Direction.North, 0, 1)]
    [InlineData(Direction.East, 1, 0)]
    [InlineData(Direction.South, 0, -1)]
    [InlineData(Direction.West, -1, 0)]
    public void FillGapMergesCorrectlyAllDirections(Direction dir, int dx, int dy)
    {
        int sx = 5, sy = 5;
        PlaceBelt(sx, sy, dir);
        PlaceBelt(sx + 2 * dx, sy + 2 * dy, dir);
        PlaceBelt(sx + dx, sy + dy, dir);

        Assert.True(CollidesAt(sx, sy));
        Assert.True(CollidesAt(sx + dx, sy + dy));
        Assert.True(CollidesAt(sx + 2 * dx, sy + 2 * dy));
    }

    [Fact]
    public void FillGapLongerPaths()
    {
        for (int x = 3; x <= 5; x++)
            PlaceBelt(x, 5, Direction.East);
        for (int x = 7; x <= 9; x++)
            PlaceBelt(x, 5, Direction.East);
        PlaceBelt(6, 5, Direction.East);

        for (int x = 3; x <= 9; x++)
            Assert.True(CollidesAt(x, 5));

        _net.Destroy(6, 5);

        Assert.True(CollidesAt(3, 5));
        Assert.True(CollidesAt(5, 5));
        Assert.False(CollidesAt(6, 5));
        Assert.True(CollidesAt(7, 5));
        Assert.True(CollidesAt(9, 5));
    }

    // ---- Destruction: endpoint removal ----

    [Fact]
    public void DestroyStartOfPathShortens()
    {
        for (int x = 5; x <= 7; x++)
            PlaceBelt(x, 5, Direction.East);

        Assert.True(_net.Destroy(5, 5));

        Assert.False(CollidesAt(5, 5));
        Assert.True(CollidesAt(6, 5));
        Assert.True(CollidesAt(7, 5));
    }

    [Fact]
    public void DestroyEndOfPathShortens()
    {
        for (int x = 5; x <= 7; x++)
            PlaceBelt(x, 5, Direction.East);

        Assert.True(_net.Destroy(7, 5));

        Assert.True(CollidesAt(5, 5));
        Assert.True(CollidesAt(6, 5));
        Assert.False(CollidesAt(7, 5));
    }

    [Theory]
    [InlineData(Direction.North, 0, 1)]
    [InlineData(Direction.East, 1, 0)]
    [InlineData(Direction.South, 0, -1)]
    [InlineData(Direction.West, -1, 0)]
    public void DestroyStartEachDirection(Direction dir, int dx, int dy)
    {
        int sx = 5, sy = 5;
        PlaceBelt(sx, sy, dir);
        PlaceBelt(sx + dx, sy + dy, dir);
        PlaceBelt(sx + 2 * dx, sy + 2 * dy, dir);

        Assert.True(_net.Destroy(sx, sy));

        Assert.False(CollidesAt(sx, sy));
        Assert.True(CollidesAt(sx + dx, sy + dy));
        Assert.True(CollidesAt(sx + 2 * dx, sy + 2 * dy));
    }

    [Theory]
    [InlineData(Direction.North, 0, 1)]
    [InlineData(Direction.East, 1, 0)]
    [InlineData(Direction.South, 0, -1)]
    [InlineData(Direction.West, -1, 0)]
    public void DestroyEndEachDirection(Direction dir, int dx, int dy)
    {
        int sx = 5, sy = 5;
        PlaceBelt(sx, sy, dir);
        PlaceBelt(sx + dx, sy + dy, dir);
        PlaceBelt(sx + 2 * dx, sy + 2 * dy, dir);

        Assert.True(_net.Destroy(sx + 2 * dx, sy + 2 * dy));

        Assert.True(CollidesAt(sx, sy));
        Assert.True(CollidesAt(sx + dx, sy + dy));
        Assert.False(CollidesAt(sx + 2 * dx, sy + 2 * dy));
    }

    // ---- Destruction: single cell ----

    [Fact]
    public void DestroySingleCellPathRemovesIt()
    {
        PlaceBelt(5, 5, Direction.East);

        Assert.True(_net.Destroy(5, 5));
        Assert.False(CollidesAt(5, 5));
    }

    [Theory]
    [InlineData(Direction.North)]
    [InlineData(Direction.East)]
    [InlineData(Direction.South)]
    [InlineData(Direction.West)]
    public void DestroySingleCellEachDirection(Direction dir)
    {
        PlaceBelt(5, 5, dir);

        Assert.True(_net.Destroy(5, 5));
        Assert.False(CollidesAt(5, 5));
    }

    // ---- Destruction: interior split ----

    [Fact]
    public void DestroyInteriorSplitsIntoTwo()
    {
        for (int x = 3; x <= 7; x++)
            PlaceBelt(x, 5, Direction.East);

        Assert.True(_net.Destroy(5, 5));

        Assert.True(CollidesAt(3, 5));
        Assert.True(CollidesAt(4, 5));
        Assert.False(CollidesAt(5, 5));
        Assert.True(CollidesAt(6, 5));
        Assert.True(CollidesAt(7, 5));
    }

    [Fact]
    public void SplitPathsAreIndependent()
    {
        for (int x = 3; x <= 7; x++)
            PlaceBelt(x, 5, Direction.East);

        _net.Destroy(5, 5);
        _net.Destroy(3, 5);

        Assert.False(CollidesAt(3, 5));
        Assert.True(CollidesAt(4, 5));
        Assert.True(CollidesAt(6, 5));
        Assert.True(CollidesAt(7, 5));
    }

    [Theory]
    [InlineData(Direction.North, 0, 1)]
    [InlineData(Direction.East, 1, 0)]
    [InlineData(Direction.South, 0, -1)]
    [InlineData(Direction.West, -1, 0)]
    public void DestroyInteriorEachDirection(Direction dir, int dx, int dy)
    {
        int sx = 5, sy = 5;
        for (int i = 0; i < 5; i++)
            PlaceBelt(sx + i * dx, sy + i * dy, dir);

        Assert.True(_net.Destroy(sx + 2 * dx, sy + 2 * dy));

        Assert.True(CollidesAt(sx, sy));
        Assert.True(CollidesAt(sx + dx, sy + dy));
        Assert.False(CollidesAt(sx + 2 * dx, sy + 2 * dy));
        Assert.True(CollidesAt(sx + 3 * dx, sy + 3 * dy));
        Assert.True(CollidesAt(sx + 4 * dx, sy + 4 * dy));
    }

    // ---- Destruction: miss ----

    [Fact]
    public void DestroyOnEmptyNetworkReturnsFalse()
    {
        Assert.False(_net.Destroy(5, 5));
    }

    [Fact]
    public void DestroyOnUnoccupiedCellReturnsFalse()
    {
        PlaceBelt(5, 5, Direction.East);

        Assert.False(_net.Destroy(8, 8));
    }

    // ---- Collision ----

    [Fact]
    public void CollidesEmptyNetworkReturnsFalse()
    {
        Assert.False(CollidesAt(5, 5));
    }

    [Fact]
    public void CollidesWithMultiCellGridPositionOverlapping()
    {
        for (int x = 5; x <= 7; x++)
            PlaceBelt(x, 5, Direction.East);

        Assert.True(_net.Collides(new GridPosition(6, 4, 2, 2)));
    }

    [Fact]
    public void CollidesWithMultiCellGridPositionNonOverlapping()
    {
        for (int x = 5; x <= 7; x++)
            PlaceBelt(x, 5, Direction.East);

        Assert.False(_net.Collides(new GridPosition(10, 10, 2, 2)));
    }

    // ---- Rebuild after destruction ----

    [Fact]
    public void PlaceBeltOnDestroyedCellSucceeds()
    {
        PlaceBelt(5, 5, Direction.East);
        _net.Destroy(5, 5);

        PlaceBelt(5, 5, Direction.East);

        Assert.True(CollidesAt(5, 5));
    }

    [Fact]
    public void DestroyAndRebuildDoesNotCorruptNetwork()
    {
        for (int x = 5; x <= 7; x++)
            PlaceBelt(x, 5, Direction.East);

        _net.Destroy(6, 5);
        PlaceBelt(6, 5, Direction.East);

        Assert.True(CollidesAt(5, 5));
        Assert.True(CollidesAt(6, 5));
        Assert.True(CollidesAt(7, 5));
    }
}
