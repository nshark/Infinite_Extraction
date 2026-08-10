using IE.Model.Primitives;

namespace IE.ViewMath;

/// <summary>
/// Grid-to-pixel conversion. Floats are legal HERE and nowhere else in the core:
/// this assembly is referenced by IE.App and the tests, but never by IE.Model or
/// IE.Controller, so sub-pixel math can never reach the deterministic state path.
/// </summary>
public static class PixelLayout
{
    /// <summary>Edge length of one grid cell, in unscaled pixels.</summary>
    public const int CellSizePixels = 64;

    /// <summary>Top-left corner of a cell, before DPI scaling.</summary>
    public static (float X, float Y) CellTopLeft(GridPosition position, float scale = 1.0f) =>
        (position.X * CellSizePixels * scale, position.Y * CellSizePixels * scale);

    /// <summary>Centre point of a cell -- where a sprite anchors.</summary>
    public static (float X, float Y) CellCenter(GridPosition position, float scale = 1.0f)
    {
        (float x, float y) = CellTopLeft(position, scale);
        float half = CellSizePixels * scale / 2.0f;
        return (x + half, y + half);
    }

    /// <summary>Pixel size of the whole board, for viewport fitting.</summary>
    public static (float Width, float Height) BoardSize(int columns, int rows, float scale = 1.0f) =>
        (columns * CellSizePixels * scale, rows * CellSizePixels * scale);

    /// <summary>Which cell a pointer at these pixel coordinates is over.</summary>
    public static GridPosition CellAt(float pixelX, float pixelY, float scale = 1.0f)
    {
        float cell = CellSizePixels * scale;
        return new GridPosition((int)MathF.Floor(pixelX / cell), (int)MathF.Floor(pixelY / cell));
    }
}
