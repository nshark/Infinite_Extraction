using IE.Controller.Views;

namespace IE.Tests.Fakes;

/// <summary>
/// Records what the controller asked to be painted. Because the view boundary is
/// plain primitives, this fake needs no engine at all -- which is the whole point.
/// </summary>
public sealed class FakeMainView : IMainView
{
    public List<MainViewModel> Renders { get; } = new();

    public MainViewModel Last => Renders[^1];

    public int RenderCount => Renders.Count;

    public void Render(MainViewModel model) => Renders.Add(model);
}
