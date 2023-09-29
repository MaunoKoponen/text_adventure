using System.Collections.Generic;

public class PlayerData
{
   
    public Dictionary<string, bool> flags = new Dictionary<string, bool>();
    
    public List<string> Inventory { get; private set; } = new List<string>();
    public Dictionary<string, bool> Flags { get; private set; } = new Dictionary<string, bool>();

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

    public bool GetFlag(string flagName)
    {
        if (Flags.TryGetValue(flagName, out bool value))
        {
            return value;
        }
        return false; // default value if flag not set
    }
}

