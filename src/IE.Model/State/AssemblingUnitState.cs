using System.Text.Json;
using System.Text.Json.Nodes;
using IE.Model.Diagnostics;

namespace IE.Model.State;

public class AssemblingUnitState : IBuildable
{
    private AuType _type;
    private int _progress;
    private RecipeData _recipe;
    public AssemblingUnitState(AuType type, RecipeId recipeId)
    {
        _type = type;
        _progress = 0;
        SetRecipe(recipeId);
    }

    public AuType GetAuType()
    {
        return _type;
    }

    public int GetProgress()
    {
        return _progress;
    }
    public void SetRecipe(RecipeId recipeId)
    {
        JsonNode? recipeData = GameData.Recipes[(int)recipeId];
        if (recipeData is not JsonObject) throw new InvalidDataException();
        _recipe = new RecipeData(recipeId, (JsonObject)recipeData);
        _progress = 0;
    }
    private struct RecipeData
    {
        private RecipeId _id;
        private int _time;
        private RContainer _inputs;
        private RContainer _outputs;
        public RecipeData(RecipeId id, JsonObject recipeData)
        {
            _id = id;
            JsonNode? node = recipeData["baseTimeSeconds"];
            if (node is not JsonValue) throw new InvalidDataException();
            _time = node.GetValue<int>();
            node = recipeData["inputs"];
            if (node is not JsonArray) throw new InvalidDataException();
            _inputs = new RContainer((JsonArray)node);
            node = recipeData["outputs"];
            if (node is not JsonArray) throw new InvalidDataException();
            _outputs = new RContainer((JsonArray)node);
        }

        public RecipeId GetID()
        {
            return _id;
        }

        public int GetTime()
        {
            return _time;
        }

        public RContainer GetInputs()
        {
            return _inputs;
        }

        public RContainer GetOutputs()
        {
            return _outputs;
        }
    }

}

