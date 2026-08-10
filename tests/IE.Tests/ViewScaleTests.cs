using IE.Model.Diagnostics;
using IE.Tests.Fakes;
using IE.ViewMath;
using Xunit;

namespace IE.Tests;

/// <summary>
/// Covers the float-allowed view-math assembly. Values here are chosen to be exactly
/// representable, so the assertions stay exact -- these are the only floats the
/// suite is allowed to reason about.
/// </summary>
public sealed class ViewScaleTests : IDisposable
{
    private readonly RecordingLogSink _sink = new();

    public ViewScaleTests()
    {
        Log.Reset();
        Log.Install(_sink);
    }

    public void Dispose() => Log.Reset();

    [Fact]
    public void ClampPassesSupportedScalesThrough()
    {
        Assert.Equal(2.0f, ViewScale.Clamp(2.0f));
        Assert.Equal(ViewScale.MinScale, ViewScale.Clamp(ViewScale.MinScale));
        Assert.Equal(ViewScale.MaxScale, ViewScale.Clamp(ViewScale.MaxScale));
    }

    [Fact]
    public void ClampPullsOutOfRangeScalesToTheBounds()
    {
        Assert.Equal(ViewScale.MinScale, ViewScale.Clamp(0.0f));
        Assert.Equal(ViewScale.MaxScale, ViewScale.Clamp(64.0f));
    }

    [Fact]
    public void ClampingIsInstrumented()
    {
        Log.SetLevel(LogCategory.View, LogLevel.Debug);

        ViewScale.Clamp(2.0f);
        Assert.False(_sink.HasCategory(LogCategory.View), "an in-range scale should log nothing");

        ViewScale.Clamp(64.0f);
        Assert.True(_sink.HasCategory(LogCategory.View), "a clamped scale should say so in the log");
    }

    [Fact]
    public void FitScaleTakesTheTighterAxis()
    {
        Assert.Equal(2.0f, ViewScale.FitScale(400, 200, 800, 800));
        Assert.Equal(2.0f, ViewScale.FitScale(200, 400, 800, 800));
    }

    [Fact]
    public void FitScaleStaysInsideTheSupportedRange()
    {
        Assert.Equal(ViewScale.MaxScale, ViewScale.FitScale(1, 1, 4096, 4096));
        Assert.Equal(ViewScale.MinScale, ViewScale.FitScale(4096, 4096, 1, 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-8)]
    public void FitScaleRejectsNonPositiveDimensions(int bad)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ViewScale.FitScale(bad, 100, 100, 100));
        Assert.Throws<ArgumentOutOfRangeException>(() => ViewScale.FitScale(100, bad, 100, 100));
        Assert.Throws<ArgumentOutOfRangeException>(() => ViewScale.FitScale(100, 100, bad, 100));
        Assert.Throws<ArgumentOutOfRangeException>(() => ViewScale.FitScale(100, 100, 100, bad));
    }

    [Fact]
    public void ToPixelsRoundsToWholeDevicePixels()
    {
        Assert.Equal(64, ViewScale.ToPixels(64, 1.0f));
        Assert.Equal(96, ViewScale.ToPixels(64, 1.5f));
        Assert.Equal(16, ViewScale.ToPixels(64, ViewScale.MinScale));
    }
}
