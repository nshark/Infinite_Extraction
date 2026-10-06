namespace IE.Model;

using System.Reflection;
using System.Text.Json.Nodes;

/// <summary>
/// Loads and caches the JSON data files embedded in this assembly.
/// Call <see cref="Load"/> once at startup; after that, index into
/// the arrays by casting an enum to int
/// (e.g. <c>GameData.Resources[(int)ResourceId.Iron]</c>).
/// </summary>
public static class GameData
{
    private static JsonArray? _resources;
    private static JsonArray? _auTypes;
    private static JsonArray? _recipes;
    private static bool _loaded;

    public static JsonArray Resources =>
        _resources ?? throw new InvalidOperationException("GameData has not been loaded.");

    public static JsonArray AuTypes =>
        _auTypes ?? throw new InvalidOperationException("GameData has not been loaded.");

    public static JsonArray Recipes =>
        _recipes ?? throw new InvalidOperationException("GameData has not been loaded.");

    public static bool IsLoaded => _loaded;

    /// <summary>
    /// Parses every embedded JSON data file and caches the result.
    /// Idempotent: subsequent calls are no-ops.
    /// </summary>
    public static void Load()
    {
        if (_loaded) return;

        Assembly assembly = typeof(GameData).Assembly;
        _resources = LoadArray(assembly, "resources.json");
        _auTypes = LoadArray(assembly, "au_types.json");
        _recipes = LoadArray(assembly, "recipes.json");
        _loaded = true;
    }

    /// <summary>
    /// Drops all cached data so the next <see cref="Load"/> re-parses.
    /// Intended for tests that need a clean slate.
    /// </summary>
    public static void Reset()
    {
        _resources = null;
        _auTypes = null;
        _recipes = null;
        _loaded = false;
    }

    private static JsonArray LoadArray(Assembly assembly, string fileName)
    {
        string resourceName = $"IE.Model.Data.{fileName}";
        using Stream? stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
            throw new InvalidOperationException($"Embedded resource not found: {resourceName}");

        JsonNode? node = JsonNode.Parse(stream);
        return node?.AsArray()
            ?? throw new InvalidOperationException($"Expected a JSON array in {fileName}.");
    }
}
