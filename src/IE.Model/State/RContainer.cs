using System.Text.Json.Nodes;

namespace IE.Model.State;

public class RContainer
{
    private Dictionary<ResourceId, int> _data;

    public RContainer(Dictionary<ResourceId, int> data)
    {
        _data = data;
    }

    public RContainer(JsonArray resourceData)
    {
        _data = new Dictionary<ResourceId, int>();
        foreach (JsonNode? node in resourceData)
        {
            if (node is not JsonObject) throw new InvalidDataException();
            JsonNode? resourceId = node["resource"];
            JsonNode? amount = node["amount"];
            if (resourceId is not JsonValue || amount is not JsonValue) throw new InvalidDataException();
            _data.Add((ResourceId)resourceId.GetValue<int>(), amount.GetValue<int>());
        }
    }

    public int GetAmount(ResourceId resourceId)
    {
        return _data.TryGetValue(resourceId, out int amount) ? amount : 0;
    }
}
