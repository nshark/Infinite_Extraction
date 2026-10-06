using System.Numerics;
using System.Text.Json.Nodes;

namespace IE.Model.State;

public class RContainer
{
    public Dictionary<ResourceId, int> Data { get; private set; }
    protected RContainer()
    {
        Data = new Dictionary<ResourceId, int>();
    }

    public RContainer(Dictionary<ResourceId, int> data)
    {
        Data = data;
    }

    public RContainer(JsonArray resourceData)
    {
        Data = new Dictionary<ResourceId, int>();
        foreach (JsonNode? node in resourceData)
        {
            if (node is not JsonObject) throw new InvalidDataException();
            JsonNode? resourceId = node["resource"];
            JsonNode? amount = node["amount"];
            if (resourceId is not JsonValue || amount is not JsonValue) throw new InvalidDataException();
            Data.Add((ResourceId)resourceId.GetValue<int>(), amount.GetValue<int>());
        }
    }
    public int GetAmount(ResourceId resourceId)
    {
        return Data.GetValueOrDefault(resourceId, 0);
    }
    public bool Contains(RContainer other)
    {
        foreach (ResourceId rid in other.Data.Keys)
        {
            if (!Data.ContainsKey(rid) || Data[rid] < other.GetAmount(rid)) {return false;}
        }
        return true;
    }
}
