
namespace IE.Model.State;

/// <summary>
/// Generic representation of a floor's belt network
/// </summary>
public class PathNetwork
{
    private Dictionary<GridPosition, Path> _paths = new Dictionary<GridPosition, Path>();
    /// <summary>
    /// entry point for place operations
    /// </summary>
    /// <param name="pos"></param>
    /// Grid position
    /// <param name="piece"></param>
    /// Piece data
    public void Place(GridPosition pos, PathComponent piece)
    {
        if (piece.Type == PathComponentType.Belt) { PlaceBeltAt(pos.X, pos.Y, piece.Facing); }
    }

    public bool Destroy(int x, int y)
    {
        if (BreakBeltAt(x, y)){return true;}

        return false;
    }
    ///this is the call when a belt is placed. It doesn't check overlaps, that happens in FloorState
    private void PlaceBeltAt(int x, int y, Direction direction)
    {
        //At most, two paths will return would extend, in which case two paths merge
        Path? p1 = null;
        Path? p2 = null;
        foreach (Path path in _paths.Values.Where(path => path.WouldExtend(x, y, direction)))
        {
            if (p1 == null)
            {
                p1 = path;
            }
            else p2 ??= path;
        }

        if (p1 == null)
        {
            Path p = new Path(x, y, direction, 1);
            _paths.Add(p.Position, p);
        }
        else if (p2 == null)
        {
            _paths.Remove(p1.Position);
            p1.Extend(x, y);
            _paths.Add(p1.Position, p1);
        }
        else
        {
            _paths.Remove(p2.Position);
            _paths.Remove(p1.Position);
            p1.Extend(x, y, Math.Abs(p2.Start.x - p2.End.x) + Math.Abs(p2.Start.y - p2.End.y) + 2);
            _paths.Add(p1.Position, p1);
        }
    }

    ///this is the call when a belt is removed
    private bool BreakBeltAt(int x, int y)
    {
        var pos = new GridPosition(x, y);

        GridPosition? key = null;
        foreach (var pathPos in _paths.Keys)
        {
            if (pathPos.Intersect(pos))
            {
                key = pathPos;
                break;
            }
        }

        if (key is null)
        {
            return false;
        }
        Path p = _paths[key];
        _paths.Remove(key);
        if (x == p.Start.x && y == p.Start.y )
        {
            if (!p.Shorten(fromStart: true))
            {
                _paths.Add(p.Position, p);
            }
            return true;
        }
        if (x == p.End.x && y == p.End.y)
        {
            if (!p.Shorten())
            {
                _paths.Add(p.Position, p);
            }
            return true;
        }
        //the split case
        int len2 = Math.Abs(x - p.End.x) + Math.Abs(y - p.End.y);
        (int x, int y) directionVector = DirectionConverter.Convert(p.Direction);
        Path p2 = new Path(x + directionVector.x, y + directionVector.y, p.Direction, len2);
        _paths.Add(p2.Position, p2);
        if (!p.Shorten(length: len2 + 1))
        {
            _paths.Add(p.Position, p);
        }
        return true;
    }

    ///This is a single 'stretch' of belt, with no turns of the same tier
    private class Path
    {
        public int Tier { get; set; }
        public GridPosition Position { get; set; }
        public (int x, int y) Start;
        public (int x, int y) End;
        public readonly Direction Direction;

        public Path(int startX, int startY, Direction direction, int length, int tier = 1)
        {
            Direction = direction;
            Start = (startX, startY);
            (int x, int y) directionVector = DirectionConverter.Convert(direction);
            End = (startX + (length - 1) * directionVector.x, startY + (length - 1) * directionVector.y);
            Position = ComputePosition(length);
            Tier = tier;
        }

        private GridPosition ComputePosition(int length)
        {
            (int x, int y) directionVector = DirectionConverter.Convert(Direction);
            return Direction is Direction.South or Direction.West
                ? new GridPosition(End.x, End.y, Math.Max(directionVector.x * length * -1, 1),
                    Math.Max(directionVector.y * length * -1, 1))
                : new GridPosition(Start.x, Start.y, Math.Max(directionVector.x * length, 1),
                    Math.Max(directionVector.y * length, 1));
        }

        public bool WouldExtend(int x, int y, Direction direction)
        {
            if (Direction != direction)
            {
                return false;
            }

            (int x, int y) directionVector = DirectionConverter.Convert(direction);
            if (x == Start.x - directionVector.x && y == Start.y - directionVector.y)
            {
                return true;
            }

            return x == End.x + directionVector.x && y == End.y + directionVector.y;
        }

        public void Extend(int x, int y, int length = 1)
        {
            (int x, int y) directionVector = DirectionConverter.Convert(Direction);
            if (x == Start.x - directionVector.x && y == Start.y - directionVector.y)
            {
                Start.x -= directionVector.x * length;
                Start.y -= directionVector.y * length;
            }
            else if (x == End.x + directionVector.x && y == End.y + directionVector.y)
            {
                End.x += directionVector.x * length;
                End.y += directionVector.y * length;
            }
            Position = ComputePosition(Math.Abs(Start.x - End.x) + Math.Abs(Start.y - End.y) + 1);
        }
        //returns true if the belt is shortened to a length of zero
        public bool Shorten(int length = 1, bool fromStart=false)
        {
            bool toReturn = Math.Abs(Start.x - End.x) + Math.Abs(Start.y - End.y) < length;
            (int x, int y) directionVector = DirectionConverter.Convert(Direction);
            if (fromStart)
            {
                Start.x += directionVector.x * length;
                Start.y += directionVector.y * length;
            }
            else
            {
                End.x -= directionVector.x * length;
                End.y -= directionVector.y * length;
            }

            Position = ComputePosition(Math.Abs(Start.x - End.x) + Math.Abs(Start.y - End.y) + 1);
            return toReturn;
        }
    }

    public bool Collides(GridPosition pos)
    {
        foreach (var pathPos in _paths.Keys)
        {
            if (pathPos.Intersect(pos)) {return true;}
        }
        return false;
    }
}
