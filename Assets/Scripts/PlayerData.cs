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
    public Dictionary<string, string> Equipments = new Dictionary<string, string>();
        
    
    // todo: equipment slots
    /*
     using enum EquipSlot
    
        Helmet,
        Neck,
        Breast,
        Pants,
        Bracers,
        Boots,
        HandMain,
        HandSecondary,
        FingerMain,
        FingerSecondary,
        None
    
     */
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

    public void EquipItem(string itemString)
    {
        
        Item itemToAdd = ItemRegistry.GetItem(itemString);
        if(itemToAdd != null)
        {
            // if slot not empty, add the item in slot to inventory
            if (Equipments.ContainsKey(itemToAdd.equipSlot.ToString()))
            {
                // put equipped item to inventory
                string itemToRemove = Equipments[itemToAdd.equipSlot.ToString()];
                
                if(itemToRemove != "none")
                    Inventory.Add(ItemRegistry.GetItem(itemToRemove));
                
                Inventory.Remove(itemToAdd);
                // add to correct slot
                Equipments[itemToAdd.equipSlot.ToString()] = itemString;
            }
            else
            {
                // add key
                Equipments.Add(itemToAdd.equipSlot.ToString(),itemString);
            }
        }
        else
        {
            Debug.LogError("Item needs to be added to dict: " + itemString);
        }
    }
    
    public void UnEquipItem(string itemString)  // remove item from slot making it empty slot
    {
        
        Item itemToRemove = ItemRegistry.GetItem(itemString);
        if(itemToRemove != null)
        {
            // if slot not empty, add the item in slot to inventory
            if (Equipments.ContainsKey(itemToRemove.equipSlot.ToString()))
            {
                Inventory.Add(itemToRemove);
                // add to correct slot
                Equipments[itemToRemove.equipSlot.ToString()] = "none";
            }
        }
        else
        {
            Debug.LogError("Item needs to be added to dict: " + itemString);
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

    
    public void SetEquipment(string equipmentSlot, string equipmentName)
    {
        Equipments[equipmentSlot] = equipmentName;
        Debug.Log("Set Flag " + equipmentName + " to " + equipmentName);
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

