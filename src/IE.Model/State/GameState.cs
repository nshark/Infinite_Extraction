namespace IE.Model.State;

public class GameState
{
    private int _time = 0;

    public FactoryFloorState Floor { get; } = new();

    public GameState()
    {

    }

    public void Tick()
    {
        _time++;
    }
}
