using IE.Model;

namespace IE.Controller.Views;

/// <summary>
/// Everything the main view is allowed to know, as plain primitives. Deliberately
/// not a reference to live state: handing the view the model would give it a way to
/// write to it, and views never mutate the model.
/// </summary>
public readonly record struct MainViewModel(
    string StatusText,
    int FloorWidth,
    int FloorHeight,
    IReadOnlyList<PlacedItemViewModel> PlacedItems);

public readonly record struct PlacedItemViewModel(
    int X,
    int Y,
    int Width,
    int Height,
    Direction Rotation,
    string TypeLabel);

/// <summary>
/// The view boundary. Implemented by the Godot scene in IE.App and by a fake in the
/// test project -- which is why it is expressed with primitives and no engine types.
/// </summary>
public interface IMainView
{
    /// <summary>Renders the whole view from scratch. Called once per state change.</summary>
    void Render(MainViewModel model);
}
