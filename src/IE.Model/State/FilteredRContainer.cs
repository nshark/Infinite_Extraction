namespace IE.Model.State;

public class FilteredRContainer : RContainer
{
    public FilteredRContainer(HashSet<ResourceId> allowed){
        foreach (var item in allowed)
        {
            Data.Add(item, 0);
        }
    }

    public FilteredRContainer(RContainer allowed)
    {
        foreach (var item in allowed.Data.Keys)
        {
            Data.Add(item, 0);
        }
    }
    public bool IsAllowed(ResourceId resource)
    {
        return Data.ContainsKey(resource);
    }

    public bool IsAllowed(RContainer other)
    {
        foreach (ResourceId rid in other.Data.Keys)
        {
            if (!Data.ContainsKey(rid)) {return false;}
        }
        return true;
    }

    public bool Insert(ResourceId resource, int amount)
    {
        if (IsAllowed(resource))
        {
            Data[resource] += amount;
            return true;
        }

        return false;
    }

    public bool Insert(RContainer container)
    {
        if (IsAllowed(container))
        {
            foreach (ResourceId resource in container.Data.Keys)
            {
                Data[resource] += container.Data[resource];
            }
            return true;
        }

        return false;
    }

    public bool Remove(ResourceId resource, int amount)
    {
        if (IsAllowed(resource) && amount <= Data[resource])
        {
            Data[resource] -= amount;
            return true;
        }
        return false;
    }

    public bool Remove(RContainer container)
    {
        if (Contains(container))
        {
            foreach (ResourceId resource in container.Data.Keys)
            {
                Data[resource] -= container.Data[resource];
            }
            return true;
        }
        return false;
    }
}
