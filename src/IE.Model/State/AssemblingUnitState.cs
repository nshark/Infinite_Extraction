using System.Text.Json;
using System.Text.Json.Nodes;
using IE.Model.Diagnostics;

namespace IE.Model.State;

public class AssemblingUnitState : IBuildable
{
    private readonly AuType _type;
    private int _progress;
    private RecipeData _recipe;
    public FilteredRContainer _inputsInv;
    public FilteredRContainer _outputsInv;
    public AssemblingUnitState(AuType type, RecipeId recipeId)
    {
        _type = type;
        _progress = 0;
        SetRecipe(recipeId);
        _inputsInv = new FilteredRContainer(_recipe.GetInputs());
        _outputsInv = new FilteredRContainer(_recipe.GetOutputs());
    }

    public AuType GetAuType()
    {
        return _type;
    }

    public void Update(int elapsedTicks)
    {
        _progress += elapsedTicks;
        while (_progress >= _recipe.GetTime())
        {
            _progress -= _recipe.GetTime();
            if (_inputsInv.Remove(_recipe.GetInputs())) {_outputsInv.Insert(_recipe.GetOutputs());}
            else
            {
                _progress %= _recipe.GetTime();
                break;
            }
        }
    }
    public int GetProgress()
    {
        return _progress;
    }
    //todo A recipe swap using this on a functioning machine would delete any items inside the AU
    public void SetRecipe(RecipeId recipeId)
    {
        JsonNode? recipeData = GameData.Recipes[(int)recipeId];
        if (recipeData is not JsonObject) throw new InvalidDataException();
        _recipe = new RecipeData(recipeId, (JsonObject)recipeData);
        _progress = 0;
        _inputsInv = new FilteredRContainer(_recipe.GetInputs());
        _outputsInv = new FilteredRContainer(_recipe.GetOutputs());
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

