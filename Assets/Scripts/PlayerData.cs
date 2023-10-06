using System.Collections.Generic;
using UnityEngine;

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

    public void RemoveItem(string itemId)
    {
        for (int i = 0; i < Inventory.Count; i++) {
            if (Inventory[i].Equals(itemId)) {
                Inventory.RemoveAt(i);   
            }
        }
    }
    
    public bool HasItem(string itemId)
    {
        return Inventory.Contains(itemId);
    }

    public void SetFlag(string flagName, bool value)
    {
        Flags[flagName] = value;
        Debug.Log("Set Flag " + flagName + " to " + value);
        
    }
    
    public bool GetFlag(string flagName)
    {
        if (Flags.ContainsKey(flagName))
        {
            return Flags[flagName];
        }
        else
        {
            // Handle the case where the flag doesn't exist.
            // This can be a Debug.Log, throw an exception, or return a default value.
            Debug.LogError("Flag " + flagName + " does not exist.");
            return false; // default value
        }
    }
    
}

