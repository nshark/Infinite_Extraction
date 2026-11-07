namespace IE.Model;

public enum Direction
{
    North,
    East,
    South,
    West,
}

public class DirectionConverter
{
    private static readonly (int, int)[] Directions = [(0, 1), (1, 0), (0, -1), (-1, 0)];
    public static (int x, int y) Convert(Direction value)
    {
        return Directions[(int)value];
    }
}

