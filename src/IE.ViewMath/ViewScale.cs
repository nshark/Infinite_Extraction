using IE.Model.Diagnostics;

namespace IE.ViewMath;

/// <summary>
/// Viewport and DPI scaling math. Floats are legal HERE and nowhere else in the
/// core: this assembly is referenced by IE.App and the tests, but never by IE.Model
/// or IE.Controller, so sub-pixel math can never reach the deterministic state path.
/// </summary>
public static class ViewScale
{
    /// <summary>Smallest scale factor the view will honour.</summary>
    public const float MinScale = 0.25f;

    /// <summary>Largest scale factor the view will honour.</summary>
    public const float MaxScale = 8.0f;

    /// <summary>
    /// Brings a requested scale into the supported range, logging when it had to be
    /// corrected -- a clamped scale usually means a bad display query upstream.
    /// </summary>
    public static float Clamp(float scale)
    {
        bool outOfRange = scale < MinScale || scale > MaxScale;
        if (outOfRange)
        {
            Log.Debug(LogCategory.View, $"view scale {scale} outside [{MinScale}, {MaxScale}]; clamping");
        }

        return Math.Clamp(scale, MinScale, MaxScale);
    }

    /// <summary>Largest clamped scale at which the content still fits the viewport.</summary>
    public static float FitScale(int contentWidth, int contentHeight, int viewportWidth, int viewportHeight)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(contentWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(contentHeight);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(viewportWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(viewportHeight);

        float horizontal = (float)viewportWidth / contentWidth;
        float vertical = (float)viewportHeight / contentHeight;
        return Clamp(MathF.Min(horizontal, vertical));
    }

    /// <summary>Converts an unscaled pixel length into scaled device pixels.</summary>
    public static int ToPixels(int unscaledLength, float scale) =>
        (int)MathF.Round(unscaledLength * Clamp(scale));
}
