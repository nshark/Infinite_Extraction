namespace IE.Model.State;

public class FactoryFloorState
{
    public Dictionary<GridPosition, IBuildable> _grid = new Dictionary<GridPosition, IBuildable>();

    public bool TryPlace(GridPosition pos, IBuildable buildable)
    {
        foreach (GridPosition gridKey in _grid.Keys)
        {
            if (gridKey.Intersect(pos))
            {
                return false;
            }
        }
        _grid.Add(pos, buildable);
        return true;
    }
    public void Remove(GridPosition pos)
    {
        _grid.Remove(pos);
    }
}
