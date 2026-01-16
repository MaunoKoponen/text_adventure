using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a slot in the inventory with an item ID and quantity.
/// Uses string itemId instead of Item reference for proper stacking and serialization.
/// </summary>
[System.Serializable]
public class InventorySlot
{
    public string itemId;
    public int quantity;

    public InventorySlot(string id, int qty = 1)
    {
        itemId = id;
        quantity = qty;
    }

    /// <summary>
    /// Get the Item definition for this slot.
    /// </summary>
    public Item GetItem()
    {
        return ItemRegistry.GetItem(itemId);
    }
}

[System.Serializable]
public class PlayerData
{
    // Legacy fields (kept for backward compatibility)
    public int health = 100;
    public int coins = 300;
    public string currentRoom;
    public List<InventorySlot> Inventory = new List<InventorySlot>();
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
    
    /// <summary>
    /// Add an item to inventory by ID. Handles stacking automatically.
    /// </summary>
    public void AddItem(string itemId, int quantity = 1)
    {
        Item itemDef = ItemRegistry.GetItem(itemId);
        if (itemDef == null)
        {
            Debug.LogError($"Item not found in registry: {itemId}");
            return;
        }

        if (itemDef.stacking)
        {
            // Find existing stack
            var slot = Inventory.Find(s => s.itemId == itemId);
            if (slot != null)
            {
                slot.quantity += quantity;
                Debug.Log($"Added {quantity} {itemId} to existing stack. Total: {slot.quantity}");
                return;
            }
        }

        // Add new slot
        Inventory.Add(new InventorySlot(itemId, quantity));
        Debug.Log($"Added new item: {itemId} x{quantity}");
    }

    /// <summary>
    /// Add an item to inventory by Item object (for backward compatibility).
    /// </summary>
    public void AddItem(Item item)
    {
        if (item == null) return;
        AddItem(item.itemId ?? item.shortDescription, 1);
    }

    /// <summary>
    /// Remove an item from inventory by ID. Returns true if successful.
    /// </summary>
    public bool RemoveItem(string itemId, int quantity = 1)
    {
        var slot = Inventory.Find(s => s.itemId == itemId);
        if (slot == null)
        {
            Debug.LogWarning($"Cannot remove item: {itemId} not in inventory");
            return false;
        }

        slot.quantity -= quantity;
        if (slot.quantity <= 0)
        {
            Inventory.Remove(slot);
            Debug.Log($"Removed last {itemId} from inventory");
        }
        else
        {
            Debug.Log($"Removed {quantity} {itemId}. Remaining: {slot.quantity}");
        }
        return true;
    }

    /// <summary>
    /// Remove an item by Item object (for backward compatibility).
    /// </summary>
    public bool RemoveItem(Item item)
    {
        if (item == null) return false;
        return RemoveItem(item.itemId ?? item.shortDescription, 1);
    }

    /// <summary>
    /// Decrease stack size (alias for RemoveItem with quantity).
    /// </summary>
    public void DecreaseStackSize(string itemId, int amount)
    {
        RemoveItem(itemId, amount);
    }

    /// <summary>
    /// Decrease stack size by Item object (for backward compatibility).
    /// </summary>
    public void DecreaseStackSize(Item item, int amount)
    {
        if (item == null) return;
        RemoveItem(item.itemId ?? item.shortDescription, amount);
    }

    /// <summary>
    /// Check if player has an item by ID.
    /// </summary>
    public bool HasItem(string itemId)
    {
        return Inventory.Exists(s => s.itemId == itemId);
    }

    /// <summary>
    /// Check if player has an item by Item object (for backward compatibility).
    /// </summary>
    public bool HasItem(Item item)
    {
        if (item == null) return false;
        return HasItem(item.itemId ?? item.shortDescription);
    }

    /// <summary>
    /// Get quantity of an item in inventory.
    /// </summary>
    public int GetItemQuantity(string itemId)
    {
        var slot = Inventory.Find(s => s.itemId == itemId);
        return slot?.quantity ?? 0;
    }

    /// <summary>
    /// Get an inventory slot by item ID.
    /// </summary>
    public InventorySlot GetInventorySlot(string itemId)
    {
        return Inventory.Find(s => s.itemId == itemId);
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

