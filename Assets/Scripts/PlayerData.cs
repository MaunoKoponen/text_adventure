using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerData
{
    // Legacy fields (kept for backward compatibility)
    public int health = 100;
    public int coins = 300;
    public string currentRoom;
    public List<Item> Inventory  = new List<Item>();
    public Dictionary<string, string> Flags = new Dictionary<string, string>();

    // New SRD 5.1 style stats (nullable for backward compatibility with old saves)
    public PlayerStats stats;
    public EquipmentSlots equipment;

    /// <summary>
    /// Check if this player data uses the enhanced stats system.
    /// </summary>
    public bool UsesEnhancedStats => stats != null;

    /// <summary>
    /// Get current HP (uses enhanced stats if available, otherwise legacy health).
    /// </summary>
    public int CurrentHP
    {
        get => stats != null ? stats.currentHitPoints : health;
        set
        {
            if (stats != null)
                stats.currentHitPoints = value;
            else
                health = value;
        }
    }

    /// <summary>
    /// Get max HP (uses enhanced stats if available, otherwise legacy health as max).
    /// </summary>
    public int MaxHP => stats != null ? stats.maxHitPoints : 100;

    /// <summary>
    /// Initialize enhanced stats if not already present.
    /// Call this when starting a new game with the enhanced system.
    /// </summary>
    public void InitializeEnhancedStats()
    {
        if (stats == null)
        {
            stats = PlayerStats.CreateDefault();
            // Sync legacy health to new system
            stats.currentHitPoints = Mathf.Min(health, stats.maxHitPoints);
        }

        if (equipment == null)
        {
            equipment = new EquipmentSlots();
        }
    }

    /// <summary>
    /// Sync enhanced stats back to legacy fields for save compatibility.
    /// </summary>
    public void SyncToLegacy()
    {
        if (stats != null)
        {
            health = stats.currentHitPoints;
        }
    }
    
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

