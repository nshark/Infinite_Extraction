using IE.Model.State;

namespace IE.Controller.Undo;

public sealed class PlaceAuEntry : IUndoEntry
{
    private readonly FactoryFloorState _floor;
    private readonly GridPosition _position;
    private readonly IBuildable _buildable;

    public PlaceAuEntry(FactoryFloorState floor, GridPosition position, IBuildable buildable)
    {
        _floor = floor;
        _position = position;
        _buildable = buildable;
    }

    public void Undo() => _floor.Remove(_position);

    public void Redo() => _floor.TryPlace(_position, _buildable);
}
