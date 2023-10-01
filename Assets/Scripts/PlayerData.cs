using System.Collections.Generic;

public class PlayerData
{
    public List<string> Inventory  = new List<string>();
    public Dictionary<string, bool> Flags = new Dictionary<string, bool>();

    public void AddItem(string itemId)
    {
        if (!Inventory.Contains(itemId))
        {
            Inventory.Add(itemId);
        }
    }

    public bool HasItem(string itemId)
    {
        return Inventory.Contains(itemId);
    }

    public void SetFlag(string flagName, bool value)
    {
        Flags[flagName] = value;
    }
    
}

