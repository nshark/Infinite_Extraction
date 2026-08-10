using System.Reflection;
using IE.Controller;
using IE.Model;
using IE.ViewMath;
using Xunit;

namespace IE.Tests.Architecture;

/// <summary>
/// Enforces the one-way dependency layering:
///
///     IE.Model  &lt;-  IE.Controller  &lt;-  (IE.App + IE.Tests)
///     IE.Model  &lt;-  IE.ViewMath    &lt;-  (IE.App + IE.Tests)
///
/// None of the three may reference the engine, and IE.Model may reference neither of
/// the other two. A new edge here is a design smell, not a test to relax.
/// </summary>
public sealed class AssemblyPurityTests
{
    private static readonly Assembly ModelAssembly = typeof(ExtractionState).Assembly;
    private static readonly Assembly ControllerAssembly = typeof(GameController).Assembly;
    private static readonly Assembly ViewMathAssembly = typeof(PixelLayout).Assembly;

    [Theory]
    [InlineData("IE.Model")]
    [InlineData("IE.Controller")]
    [InlineData("IE.ViewMath")]
    public void CoreAssembliesNeverReferenceTheEngine(string assemblyName)
    {
        Assembly assembly = Resolve(assemblyName);

        string[] engineReferences = References(assembly)
            .Where(name => name.Contains("Godot", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.True(
            engineReferences.Length == 0,
            $"{assemblyName} picked up an engine reference ({string.Join(", ", engineReferences)}). " +
            "Framework-side code belongs in IE.App.");
    }

    [Fact]
    public void ModelReferencesNoOtherProjectAssembly()
    {
        string[] projectReferences = ProjectReferences(ModelAssembly);

        Assert.True(
            projectReferences.Length == 0,
            $"IE.Model must sit at the bottom of the graph but references: {string.Join(", ", projectReferences)}");
    }

    [Fact]
    public void ControllerReferencesModelOnly()
    {
        Assert.Equal(new[] { "IE.Model" }, ProjectReferences(ControllerAssembly));
    }

    [Fact]
    public void ViewMathReferencesModelOnly()
    {
        Assert.Equal(new[] { "IE.Model" }, ProjectReferences(ViewMathAssembly));
    }

    private static Assembly Resolve(string assemblyName) => assemblyName switch
    {
        "IE.Model" => ModelAssembly,
        "IE.Controller" => ControllerAssembly,
        "IE.ViewMath" => ViewMathAssembly,
        _ => throw new ArgumentOutOfRangeException(nameof(assemblyName), assemblyName, "unknown assembly"),
    };

    private static string[] References(Assembly assembly) =>
        assembly.GetReferencedAssemblies()
            .Select(name => name.Name ?? string.Empty)
            .ToArray();

    private static string[] ProjectReferences(Assembly assembly) =>
        References(assembly)
            .Where(name => name.StartsWith("IE.", StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
}
