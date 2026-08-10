namespace IE.Model.Primitives;

/// <summary>
/// A cell on the extraction grid. Integer-only by rule: no float ever enters
/// IE.Model, because float math is not bit-identical across platforms and the core
/// state must stay deterministic. Pixel-space conversion lives in IE.ViewMath.
/// </summary>
public readonly record struct GridPosition(int X, int Y)
{
    public static GridPosition Origin => new(0, 0);

    public GridPosition Offset(int dx, int dy) => new(X + dx, Y + dy);

    /// <summary>Manhattan distance -- integer by construction, unlike a Euclidean one.</summary>
    public int ManhattanDistanceTo(GridPosition other) =>
        Math.Abs(X - other.X) + Math.Abs(Y - other.Y);

    public override string ToString() => $"({X},{Y})";
}
