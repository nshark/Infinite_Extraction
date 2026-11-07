namespace IE.Model.State;

public class PathComponent(PathComponentType pathComponentType, Direction facing) : IBuildable
{
    public PathComponentType Type { get; } = pathComponentType;
    public Direction Facing = facing;
}

