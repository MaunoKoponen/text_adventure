using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerData
{
    public int health = 100;
    public int coins = 300;
    public string currentRoom;
    public List<Item> Inventory  = new List<Item>();
    public Dictionary<string, string> Flags = new Dictionary<string, string>();
    
    public void AddItem(Item item)
    {
        // Depending on stacking logic, you might want to check if the item already exists
        // and increase its quantity, etc.
        Inventory.Add(item);
    }
    
    public void AddItem(string item)
    {
        
        Item itemToAdd = ItemRegistry.GetItem(item);
        if(itemToAdd != null)
        {
            Inventory.Add(itemToAdd);
        }
        else
        {
            Debug.LogError("Item needs to be added to dict: " + item);
        }
    }

    public void RemoveItem(string item)
    {
        
        Item itemToAdd = ItemRegistry.GetItem(item);
        if(itemToAdd != null)
        {
            Inventory.Remove(itemToAdd);
        }
        else
        {
            Debug.LogError("Item needs to be added to dict: " + item);
        }
    }
    
    public void RemoveItem(Item item)
    {
        for (int i = 0; i < Inventory.Count; i++) {
            if (Inventory[i].Equals(item)) {
                Inventory.RemoveAt(i);   
            }
        }
    }
    
    public void DecreaseStackSize(Item item, int amount)
    {
        for (int i = 0; i < Inventory.Count; i++) {
            if (Inventory[i].Equals(item))
            {
                Inventory[i].count -= amount;

                if (Inventory[i].count <= 0)
                {
                    Inventory.RemoveAt(i); 
                }
            }
        }
    }

    
    public bool HasItem(Item item)
    {
        return Inventory.Contains(item);
    }

    public void SetFlag(string flagName, string value)
    {
        Flags[flagName] = value;
        Debug.Log("Set Flag " + flagName + " to " + value);
        
    }
    
    public string GetFlag(string flagName)
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
            return "false"; // default value
        }
    }
    
}

