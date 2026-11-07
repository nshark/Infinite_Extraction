using IE.Model;
using IE.Model.State;
using Xunit;

namespace IE.Tests;

public sealed class GridPositionTests
{
    [Fact]
    public void IdenticalPositionsAreEqual()
    {
        GridPosition a = new(3, 7, 2, 2);
        GridPosition b = new(3, 7, 2, 2);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void DifferentOriginIsNotEqual()
    {
        GridPosition a = new(0, 0, 2, 2);
        GridPosition b = new(1, 0, 2, 2);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void DifferentSizeSameOriginIsNotEqual()
    {
        GridPosition small = new(5, 5, 1, 1);
        GridPosition big = new(5, 5, 2, 2);

        Assert.NotEqual(small, big);
    }

    [Fact]
    public void DefaultConstructorOneByOneEqualsExplicitOneByOne()
    {
        GridPosition shortForm = new(4, 8);
        GridPosition longForm = new(4, 8, 1, 1);

        Assert.Equal(shortForm, longForm);
    }

    [Fact]
    public void DictionaryLookupByValueWorks()
    {
        Dictionary<GridPosition, string> dict = new();
        GridPosition key = new(2, 3, 2, 2);
        dict[key] = "smelter";

        GridPosition lookup = new(2, 3, 2, 2);

        Assert.True(dict.ContainsKey(lookup));
        Assert.Equal("smelter", dict[lookup]);
    }

    [Fact]
    public void DictionaryRemoveByValueWorks()
    {
        Dictionary<GridPosition, string> dict = new();
        GridPosition original = new(10, 10, 3, 3);
        dict[original] = "assembler";

        GridPosition removeKey = new(10, 10, 3, 3);
        bool removed = dict.Remove(removeKey);

        Assert.True(removed);
        Assert.Empty(dict);
    }

    [Fact]
    public void NullIsNotEqual()
    {
        GridPosition pos = new(0, 0);

        Assert.False(pos.Equals(null));
    }

    [Fact]
    public void DifferentTypeIsNotEqual()
    {
        GridPosition pos = new(0, 0);

        Assert.False(pos.Equals("not a grid position"));
    }

    [Fact]
    public void EqualityIsSymmetric()
    {
        GridPosition a = new(7, 3, 4, 2);
        GridPosition b = new(7, 3, 4, 2);

        Assert.Equal(a.Equals(b), b.Equals(a));
    }

    [Fact]
    public void EqualityIsTransitive()
    {
        GridPosition a = new(1, 2, 3, 3);
        GridPosition b = new(1, 2, 3, 3);
        GridPosition c = new(1, 2, 3, 3);

        Assert.Equal(a, b);
        Assert.Equal(b, c);
        Assert.Equal(a, c);
    }

    [Fact]
    public void DistinctPositionsCanShareHashWithoutBreakingDictionary()
    {
        GridPosition a = new(5, 5, 1, 1);
        GridPosition b = new(5, 5, 2, 2);

        Dictionary<GridPosition, string> dict = new();
        dict[a] = "belt";
        dict[b] = "smelter";

        Assert.Equal(2, dict.Count);
        Assert.Equal("belt", dict[a]);
        Assert.Equal("smelter", dict[b]);
    }
}
