namespace IE.Controller.Views;

/// <summary>
/// Everything the main view is allowed to know, as plain primitives. Deliberately
/// not a reference to <c>ExtractionState</c>: handing the view the live model would
/// give it a way to write to it, and views never mutate the model.
/// </summary>
public readonly record struct MainViewModel(
    int OreRemaining,
    int OreExtracted,
    int DrillPower,
    int DrillX,
    int DrillY,
    bool CanExtract,
    bool AutoExtractEnabled,
    bool CanUndo,
    string StatusText);

/// <summary>
/// The view boundary. Implemented by the Godot scene in IE.App and by a fake in the
/// test project -- which is why it is expressed with primitives and no engine types.
/// </summary>
public interface IMainView
{
    /// <summary>Renders the whole view from scratch. Called once per state change.</summary>
    void Render(MainViewModel model);

    /// <summary>One-shot feedback (animation, sound) for a completed extraction tick.</summary>
    void PlayExtractionFeedback(int yield);
}
