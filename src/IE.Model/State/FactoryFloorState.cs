namespace IE.Model.State;

public class FactoryFloorState
{
    // does not contain PathComponents
    public Dictionary<GridPosition, IBuildable> _grid = new Dictionary<GridPosition, IBuildable>();
    public PathNetwork BeltNetwork = new PathNetwork();
    public List<AssemblingUnitState> AssemblingUnitStates = new List<AssemblingUnitState>();
    public bool TryPlace(GridPosition pos, IBuildable buildable)
    {
        if (BeltNetwork.Collides(pos)) { return false; }
        foreach (GridPosition gridKey in _grid.Keys)
        {
            if (gridKey.Intersect(pos))
            {
                return false;
            }
        }

        if (buildable is AssemblingUnitState state)
        {
            AssemblingUnitStates.Add(state);
            _grid.Add(pos, buildable);
            return true;
        }
        else if (buildable is PathComponent pathComponent)
        {
            BeltNetwork.Place(pos, pathComponent);
            return true;
        }
        return false;
    }
    public void Remove(GridPosition pos)
    {
        if (BeltNetwork.Destroy(pos.X, pos.Y)){return;}
        if (_grid[pos] is AssemblingUnitState state)
        {
            AssemblingUnitStates.Remove(state);
        }
        _grid.Remove(pos);
    }

    public void Update(int ticksElapsed)
    {
        foreach (AssemblingUnitState assemblingUnitState in AssemblingUnitStates)
        {
            assemblingUnitState.Update(ticksElapsed);
        }
    }
}
