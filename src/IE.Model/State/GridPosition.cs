namespace IE.Model.State;

public class GridPosition
{
    public int X { get; }
    public int Y { get; }
    public int Width { get; }
    public int Height { get; }

    public GridPosition(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public GridPosition(int x, int y)
    {
        X = x;
        Y = y;
        Width = 1;
        Height = 1;
    }
    public Boolean Intersect(int x, int y)
    {
        if (X <= x && X + Width >= x)
        {
            return true;
        }

        if (Y <= y && Y + Height >= y)
        {
            return true;
        }
        return false;
    }

    public Boolean Intersect(GridPosition other)
    {
        return X < other.X + other.Width && X + Width > other.X && Y < other.Y + other.Height && Y + Height > other.Y;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType()) { return false; }
        var other = (GridPosition)obj;
        return X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;
    }
}
