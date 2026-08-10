namespace IE.Controller.Views;

/// <summary>
/// Everything the main view is allowed to know, as plain primitives. Deliberately
/// not a reference to live state: handing the view the model would give it a way to
/// write to it, and views never mutate the model.
///
/// The shell carries a status line and nothing else. Gameplay fields get added here
/// as they are built, one per thing the view actually needs to paint.
/// </summary>
public readonly record struct MainViewModel(string StatusText);

/// <summary>
/// The view boundary. Implemented by the Godot scene in IE.App and by a fake in the
/// test project -- which is why it is expressed with primitives and no engine types.
/// </summary>
public interface IMainView
{
    /// <summary>Renders the whole view from scratch. Called once per state change.</summary>
    void Render(MainViewModel model);
}
