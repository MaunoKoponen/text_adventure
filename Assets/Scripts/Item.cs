using System.Collections.Generic;
using UnityEngine.Serialization;

[System.Serializable]
public class Item
{
    public string itemId;           // Unique identifier for the item (used for stacking and JSON loading)
    public string usageSuccess;
    public string usageFail;
    public string description;
    public string shortDescription;
    public int buyPrice;
    public int sellPrice;
    public EffectType effectType;
    public int effectAmount;        // This is to denote the strength/quantity of the effect
    public string category;
    public Target target;
    public bool stacking;
    public int maxStack = 99;       // Maximum stack size (for stacking items)
    public int count = 0;           // Deprecated: use InventorySlot.quantity instead
    public string image;
    public bool combatUsable = true; // Whether this item can be used in combat
    public enum EffectType
    {
        Damage,
        Heal,
        Bless,
        CurePoison,
        Open
        // ... More effects here.
    }

    public enum Target
    {
        NPC,
        Self,
        Lock,
        None
    }

}


/// <summary>
/// Registry for loading and caching items from JSON.
/// All items are now data-driven - loaded from Stories/{storyId}/Items/ or Items/ folders.
/// </summary>
public static class ItemRegistry
{
    // Items indexed by itemId (primary) and shortDescription (legacy compatibility)
    private static Dictionary<string, Item> itemsById = new Dictionary<string, Item>();
    private static Dictionary<string, Item> itemsByName = new Dictionary<string, Item>();

    /// <summary>
    /// Register an item in both dictionaries (for caching loaded items).
    /// </summary>
    public static void RegisterItem(Item item)
    {
        if (item == null) return;

        if (!string.IsNullOrEmpty(item.itemId))
            itemsById[item.itemId] = item;

        if (!string.IsNullOrEmpty(item.shortDescription))
            itemsByName[item.shortDescription] = item;
    }

    /// <summary>
    /// Get an item by itemId or shortDescription.
    /// Priority: 1) Cache, 2) JSON from StoryManager
    /// </summary>
    public static Item GetItem(string itemKey)
    {
        if (string.IsNullOrEmpty(itemKey)) return null;

        // Try cache first (by itemId)
        if (itemsById.TryGetValue(itemKey, out Item itemById))
            return itemById;

        // Try cache by shortDescription (legacy)
        if (itemsByName.TryGetValue(itemKey, out Item itemByName))
            return itemByName;

        // Load from JSON via StoryManager
        if (StoryManager.Instance != null)
        {
            Item jsonItem = StoryManager.Instance.LoadItemData(itemKey);
            if (jsonItem != null)
            {
                RegisterItem(jsonItem); // Cache for future lookups
                return jsonItem;
            }
        }

        UnityEngine.Debug.LogWarning($"Item not found in registry: {itemKey}");
        return null;
    }

    /// <summary>
    /// Clear the registry (useful when switching stories).
    /// </summary>
    public static void Clear()
    {
        itemsById.Clear();
        itemsByName.Clear();
    }
}